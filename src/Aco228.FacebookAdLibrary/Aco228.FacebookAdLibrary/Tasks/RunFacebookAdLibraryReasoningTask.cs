using System.Data;
using System.Text;
using Aco228.AIGen.Helpers;
using Aco228.Common.Attributes;
using Aco228.Common.Extensions;
using Aco228.FacebookAdLibrary.Documents;
using Aco228.FacebookAdLibrary.Prompts.ExamineCompetitors;
using Aco228.FacebookAdLibrary.Prompts.RemoveDuplicates;
using Aco228.MongoDb.Extensions;
using Aco228.MongoDb.Extensions.MongoFiltersExtensions;
using Aco228.MongoDb.Extensions.RepoExtensions;
using Aco228.MongoDb.Services;
using Aco228.Runners.Core.Tasks;

namespace Aco228.FacebookAdLibrary.Tasks;

public class RunFacebookAdLibraryReasoningTask : TaskBase
{
    [InjectService] public IMongoRepo<FbLibAdDocument> AdRepo { get; set; }
    [InjectService] public IMongoRepo<FbLibCompetitorDocument> CompetitorsRepo { get; set; }
    
    protected override async Task InternalExecute()
    {
        
    }

    public async Task RunForDays(int days, int contextLength)
    {
        var daysDiff = DateTime.UtcNow.AddDays(0 - days).ToUnixTimestampSeconds();
        var ads = await AdRepo.NoTrack().Full()
            .Lte(x => x.StartDate, daysDiff)
            .ToListAsync();
        
        if(!ads.Any())
            return;

        var results = await CompetitorsRepo.NoTrack().Full().ToListAsync();
        var usedIds = results.Select(x => x.AdIds).GetAllListsCombined();
        
        var candidates = ads.Where(x => !usedIds.Contains(x.Id)).Shuffle().Take(contextLength).ToList();

        await AiAnalyise(candidates, results);

        var removeDuplicatesPrompt = PromptHelper.Get<RemoveDuplicatesPrompt>();

        var maximum = (int) Math.Ceiling(contextLength * 1.0 / 2.0);
        var removeDuplicatesPromptRequest = new RemoveDuplicatesPromptRequest() { Maximum = maximum, Descriptions = results.Select(x => x.Description).ToList()};
        var removeDuplicatesPromptResult = await removeDuplicatesPrompt.Execute(removeDuplicatesPromptRequest);

        var toDelete = new List<FbLibCompetitorDocument>();
        foreach (var duplicate in removeDuplicatesPromptResult)
        {
            var original = results.FirstOrDefault(x => x.Description == duplicate.Original);
            if (original != null)
            {
                var duplicates = results.Where(x => duplicate.Duplicates.Contains(x.Description)).ToList();
                if (duplicates.Any())
                {
                    original.AdIds.AddRange(duplicates.Select(x => x.AdIds).GetAllListsCombined().Distinct());
                    original.AdIds = original.AdIds.Distinct().ToList();
                    toDelete.AddRange(duplicates);
                }
            }
        }


        await CompetitorsRepo.DeleteManyAsync(toDelete);
        await CompetitorsRepo.InsertOrUpdateManyAsync(results);
    }

    private static async Task AiAnalyise(List<FbLibAdDocument> candidates, List<FbLibCompetitorDocument> results)
    {
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Id,Body,Title");
        int currentId = 0;
        
        foreach (var ad in candidates)
        {
            currentId++;
            var variation = ad.Variations.First();
            var body = variation.Body?.Trim().Replace("\n", " ").Replace("\r\n", " ").Replace(Environment.NewLine, " ") ?? string.Empty;
            var title = variation.Title?.Trim().Replace("\n", " ").Replace("\r\n", " ").Replace(Environment.NewLine, " ") ?? string.Empty;
            if (string.IsNullOrEmpty(body) && string.IsNullOrEmpty(title))
                continue;
         
            csvBuilder.AppendLine($"{currentId},{body},{title}");
        }

        var csv = csvBuilder.ToString();
        var prompt = PromptHelper.Get<ExamineCompetitorsPrompt>();
        var promptRequest = new ExamineCompetitorsPromptRequest() { Csv = csv };
        var promptText = await prompt.GetPromptText(promptRequest);
        var promptRes = await prompt.Execute(promptRequest);

        foreach (var entry in promptRes)
        {
            var competitor = new FbLibCompetitorDocument()
            {
                Title = entry.Title,
                CandidateCountryCodes = entry.CandidateCountryCodes,
                Description = entry.Description,
                LanguageCode = entry.LanguageCode,
                Vertical = entry.Vertical,
            };

            foreach (var id in entry.Ids)
            {
                var ad = candidates.TryGetElementAt(id - 1);
                if(ad != null)
                    competitor.AdIds.Add(ad.Id);
            }
            
            results.Add(competitor);
        }
    }
}