using Aco228.AIGen.Attributes;
using Aco228.AIGen.Models;
using Aco228.AIGen.Services;
using Aco228.Common.Infrastructure;

namespace Aco228.FacebookAdLibrary.Prompts.RemoveDuplicates;

public class RemoveDuplicatesPrompt : PromptBase<RemoveDuplicatesPromptRequest, List<RemoveDuplicatesPromptResponse>>
{
    protected override string PromptName => "fb.removeDuplicates.v1";
    protected override ManagedList<TextGenProvider>? TextGenProviders => new()
    {
        TextGenProvider.ChatGPT,
        TextGenProvider.Claude,
        TextGenProvider.Gemini,
    };
}

public class RemoveDuplicatesPromptRequest
{
    [PromptHint("Maximum number of responses (this is maximum number, if there is not enough data, you can produce less)")]
    public int Maximum { get; set; }
    public List<string> Descriptions { get; set; }
}

public class RemoveDuplicatesPromptResponse
{
    [PromptHint("Original description")]
    public string Original { get; set; }
    
    [PromptHint("Duplicates of that original description")]
    public List<string> Duplicates { get; set; }
}