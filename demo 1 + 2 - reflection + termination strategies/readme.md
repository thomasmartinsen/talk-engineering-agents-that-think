# Python demos

These demos are based on the [multi-agent sample provided in the Semantic Kernel repository](https://github.com/microsoft/semantic-kernel/blob/main/python/samples/getting_started_with_agents/azure_ai_agent/step3_azure_ai_agent_group_chat.py). I am using Azure AI Agent Service to deploy the agents, however, it is possible to use other APIs, such as the chat completion, Assistants, or Responses API. Check the sibling folders for corresponding samples.

# Demo 1: Reflection
Demo 1 implements a simple worker-reviewer pattern to produce a multi-agent system that is capable of considering a task from multiple angles to arrive at a more complete solution.

# Demo 2.1: Rogue termination
Demo 2.1 demonstrates the importance of termination strategy and to be selective about which agents to apply it to. 

# Demo 2.2: Deterministic termination
Demo 2.2 shows an alternative approach for terminating group chats with the help of deterministic checks as opposed to LLM-generated output.