using Spectre.Console;

internal static class UITools
{
    public static async Task<string> ConfirmAnswer(Conversation conversation, string initialAnswer, string regeneratePrompt)
    {
        var regenerate = true;
        var answer = initialAnswer;
        while (regenerate)
        {
            AnsiConsole.Write(new Panel(answer)
            {
                Header = new PanelHeader("Output")
            });

            regenerate = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Do you like [green]output[/]?")
                    .AddChoices(["Yes", "No"])) == "No";

            if (!regenerate) break;

            answer = await conversation.Say(regeneratePrompt);
        }
        return answer;
    }
}
