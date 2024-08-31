# Git Commit Message Generator

This project is a command-line tool designed to automate the generation of Git commit messages using AI-powered suggestions. It utilizes the Microsoft Semantic Kernel along with OpenAI or Azure OpenAI to generate commit messages based on the diff of staged changes in your Git repository.

## Features

- **Automatic Git Commit Messages:** Automatically generate descriptive commit messages based on the changes staged in your repository.
- **AI Integration:** Utilize AI models from OpenAI or Azure OpenAI for generating meaningful commit messages.
- **Customizable Prompts:** Modify and customize the prompt templates for different commit scenarios.
- **Debugging Support:** Option to display detailed information during the commit message generation process.

## Dependencies

- [.NET 7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [Spectre.Console](https://spectreconsole.net/)
- [Dotenv.net](https://github.com/bolorundurowb/dotenv.net)
- OpenAI or Azure OpenAI API key

## Getting Started

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/your-username/git-commit-message-generator.git
   cd git-commit-message-generator
   ```

2. Install the necessary dependencies:

   ```bash
   dotnet restore
   ```

3. Set up your environment variables by creating a .env file:

   ```env
   AI_PROVIDER=GROQ # or AZURE_OPENAI
   GROQ_ENDPOINT=https://api.openai.com/v1
   GROQ_MODEL_ID=text-davinci-003
   GROQ_API_KEY=your-openai-api-key
   AZURE_OPENAI_DEPLOYMENT=your-deployment-name
   AZURE_OPENAI_ENDPOINT=your-endpoint
   AZURE_OPENAI_API_KEY=your-api-key
   WORKING_DIR=path/to/your/repository
   ```

### Usage

1. Run the application with the commit command:

   ```bash
   dotnet run -- commit
   ```

2. Follow the prompts to generate and review the commit message.

3. The application will commit your staged changes with the generated message.

### Debugging

To enable debug mode, add the --debug flag:

   ```bash
   dotnet run -- commit --debug
   ```
