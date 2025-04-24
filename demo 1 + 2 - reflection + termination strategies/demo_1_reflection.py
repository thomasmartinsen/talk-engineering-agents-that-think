import asyncio

from azure.identity.aio import DefaultAzureCredential

from semantic_kernel.agents import AgentGroupChat, AzureAIAgent, AzureAIAgentSettings
from semantic_kernel.agents.strategies import TerminationStrategy
from semantic_kernel.contents import AuthorRole


class ApprovalTerminationStrategy(TerminationStrategy):
    """A strategy for determining when an agent should terminate."""

    async def should_agent_terminate(self, agent, history):
        """Check if the agent should terminate."""
        return "i declare this problem solved" in history[-1].content.lower()


REVIEWER_NAME = "MathProfessor"
REVIEWER_INSTRUCTIONS = """
You are an old-school math professor.
The goal is to find the solution to a math problem.
Consider your colleague's suggestions and incorporate them into your solution.
If you have considered your colleague's feedback, summarize the combined solution and
state the following phrase: "I declare this problem solved!"
You don't need to wait for conensus from your colleague.
When you say the phrase, the conversation will immediately end.
Do not use the phrase until you are done!
If not, discuss with your colleague on how to solve the problem.
"""

BINARY_NAME = "ComputerScienceProfessor"
BINARY_INSTRUCTIONS = """
You are a computer science professor that always prefers operating in binary.
Provide your feedback on the problem to solve.
"""

TASK = "1+1"


async def main():
    ai_agent_settings = AzureAIAgentSettings()

    async with (
        DefaultAzureCredential() as creds,
        AzureAIAgent.create_client(credential=creds) as client,
    ):
        # 1. Create the first agent on the Azure AI agent service
        reviewer_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=REVIEWER_NAME,
            instructions=REVIEWER_INSTRUCTIONS,
        )

        # 2. Create a Semantic Kernel agent for the first Azure AI agent
        reviewer_agent = AzureAIAgent(
            client=client,
            definition=reviewer_agent_definition,
        )

        # 3. Create the second agent on the Azure AI agent service
        binary_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=BINARY_NAME,
            instructions=BINARY_INSTRUCTIONS,
        )

        # 4. Create a Semantic Kernel agent for the second Azure AI agent
        binary_agent = AzureAIAgent(
            client=client,
            definition=binary_agent_definition,
        )

        # 5. Place the agents in a group chat with a custom termination strategy
        chat = AgentGroupChat(
            agents=[reviewer_agent, binary_agent],
            termination_strategy=ApprovalTerminationStrategy(agents=[reviewer_agent], maximum_iterations=10),
        )

        try:
            # 6. Add the task as a message to the group chat
            await chat.add_chat_message(message=TASK)
            print(f"# {AuthorRole.USER}: '{TASK}'")
            # 7. Invoke the chat
            async for content in chat.invoke():
                print(f"\n# {content.name or '*'}: '{content.content}'")
        finally:
            # 8. Cleanup: Delete the agents
            await chat.reset()
            await client.agents.delete_agent(reviewer_agent.id)
            await client.agents.delete_agent(binary_agent.id)


if __name__ == "__main__":
    asyncio.run(main())