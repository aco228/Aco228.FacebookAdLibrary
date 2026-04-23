using Aco228.AIGen.Attributes;
using Aco228.AIGen.Models;
using Aco228.AIGen.Services;
using Aco228.Common.Infrastructure;
using Toon;

namespace Aco228.FacebookAdLibrary.Prompts.CreateSuggestions;

public class CreateSuggestionsPrompt : PromptBase<CreateSuggestionsPromptRequest, CreateSuggestionsPromptResponse>
{
    protected override string PromptName => "createSuggestions.v1.prompt";

    protected override ManagedList<TextGenProvider>? TextGenProviders => new()
    {
        TextGenProvider.Claude,
        TextGenProvider.Gemini,
    };

    protected override async Task<string> ModifySystemPrompt(string systemPrompt, CreateSuggestionsPromptRequest request)
    {
        return systemPrompt
            .Replace("{COUNTRIES_BLACKLIST}", (request.IgnoreCountries == null || !request.IgnoreCountries.Any() ? "" : "- Do not use these countries in the response: " + string.Join(", ", request.IgnoreCountries)))
            .Replace("{COUNTRIES_WHITELIST}", (request.OnlyCountries == null || !request.OnlyCountries.Any() ? "" : "- Use only these countries in the response: " + string.Join(", ", request.IgnoreCountries)))
            .Replace("{{SPY_ADS}}", ToonEncoder.Encode(request.Entries));
    }
}

public class CreateSuggestionsPromptRequestEntry
{
    [PromptHint("the ad headline")]
    public string Title { get; set; }
    
    [PromptHint("the ad body")]
    public string Body { get; set; }
    
    [PromptHint("guess of the language the ad is written in")]
    public string Language { get; set; }
}

public class CreateSuggestionsPromptRequest
{
    [PromptIgnore]public required List<CreateSuggestionsPromptRequestEntry> Entries { get; set; }
    
    [PromptHint("The number of suggestions to generate")]
    public required int Count { get; set; }
    
    [PromptHint("Ignore these countries in response")]
    public required List<string>? IgnoreCountries { get; set; } = null;
    
    [PromptHint("Use only there countries in response")]
    public required List<string>? OnlyCountries { get; set; } = null;
    
    [PromptHint("A list of vertical names I currently support")]
    public required List<CreateSuggestionsPromptRequestVertical> SupportedVerticals { get; set; }
}

public class CreateSuggestionsPromptRequestVertical
{
    [PromptHint("vertical name")]
    public string VerticalName { get; set; }
    
    [PromptHint("Intent of this article")]
    public string Intent { get; set; }
    
    [PromptHint("The following are real titles currently in use. Study their style, length, specificity, and hook type. All generated titles must match this standard.")]
    public List<string> TitleExamples { get; set; }
}

public class CreateSuggestionsPromptResponse
{
    [PromptHint("Ad suggestion")]
    public List<CreateSuggestionsPromptResponseSuggestion> AdSuggestions { get; set; }
    
    [PromptHint("Vertical suggestions that are not listed")]
    public List<string> VerticalSuggestions { get; set; }
}

public class CreateSuggestionsPromptResponseSuggestion
{
    [PromptHint("Vertical name matched from user prompt")]
    public string Vertical { get; set; }
    
    [PromptHint("TwoLetterIso language code of the ad")]
    public string LanguageCode { get; set; }
    
    [PromptHint("TwoLetterIso country code this ad is supposed for based on the context")]
    public string CountryCode { get; set; }
    public string Title { get; set; }
}