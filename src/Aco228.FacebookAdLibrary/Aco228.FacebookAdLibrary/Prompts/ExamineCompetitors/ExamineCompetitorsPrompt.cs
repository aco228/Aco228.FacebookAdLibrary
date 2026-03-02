using Aco228.AIGen.Attributes;
using Aco228.AIGen.Models;
using Aco228.AIGen.Services;
using Aco228.Common.Infrastructure;

namespace Aco228.FacebookAdLibrary.Prompts.ExamineCompetitors;

public class ExamineCompetitorsPrompt : PromptBase<ExamineCompetitorsPromptRequest, List<ExamineCompetitorsPromptResponse>>
{
    protected override string PromptName => "examineCompetitors.v1";
    protected override ManagedList<TextGenProvider>? TextGenProviders => new()
    {
        TextGenProvider.ChatGPT,
        TextGenProvider.Claude,
        TextGenProvider.Gemini,
    };
}

public class ExamineCompetitorsPromptRequest
{
    public string Csv { get; set; }
}

public class ExamineCompetitorsPromptResponse
{
    [PromptHint("Single word thematic category this ad belongs to (e.g. \"Health\", \"Jobs\", \"Scholarships\")")]
    public string Vertical { get; set; }
    
    [PromptHint("Language in which that group of offers is written in (TwoLetterIsoCode)")]
    public string LanguageCode { get; set; }
    
    [PromptHint(
        "Maximum 3 ISO 3166-1 alpha-2 country codes where this ad would perform well. ",
        "Take into account in which language it is written.",
        "If only one country speaks that language, its okay to return just one or two countries.")]
    public List<string> CandidateCountryCodes { get; set; }
    
    [PromptHint("1-2 sentence neutral description of what this ad is offering or communicating in english. Description must always be in english. Do not editorialize.")]
    public string Description { get; set; }
    
    [PromptHint("A clean, concise article/ad title you would assign to this offer (in original language, not necessarily copied from the original,)")]
    public string Title { get; set; }
    
    [PromptHint("Based on which ids you made this proposal (use Id from the user prompt)")]
    public List<int> Ids { get; set; }
}