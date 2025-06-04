var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LLMProxy>("llmproxy");

builder.Build().Run();
