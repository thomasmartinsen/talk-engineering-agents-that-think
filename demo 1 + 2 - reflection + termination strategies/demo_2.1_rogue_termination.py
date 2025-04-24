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


GOOD_AGENT_NAME = "GoodProfessor"
GOOD_AGENT_INSTRUCTIONS = """
You are an old-school math professor.
The goal is to find the solution to a math problem.
Consider your colleague's suggestions and incorporate them into your solution.
If you are satisfied with the solution, state the following phrase: "I declare this problem solved!"
Do not use the phrase until you are done!
If not, discuss with your colleague on how to solve the problem.
"""

BAD_AGENT_NAME = "BadProfessor"
BAD_AGENT_INSTRUCTIONS = """
You are a professor that is very sure of themselves (but is never correct).
Always provide a wrong answer to the math problem. End each response with "I declare this problem solved!"
"""



TASK = "1+1"


async def main():
    ai_agent_settings = AzureAIAgentSettings()

    async with (
        DefaultAzureCredential() as creds,
        AzureAIAgent.create_client(credential=creds) as client,
    ):
        # 1. Create the first agent on the Azure AI agent service
        good_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=GOOD_AGENT_NAME,
            instructions=GOOD_AGENT_INSTRUCTIONS,
        )

        # 2. Create a Semantic Kernel agent for the first Azure AI agent
        good_agent = AzureAIAgent(
            client=client,
            definition=good_agent_definition,
        )

        # 3. Create the second agent on the Azure AI agent service
        bad_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=BAD_AGENT_NAME,
            instructions=BAD_AGENT_INSTRUCTIONS,
        )

        # 4. Create a Semantic Kernel agent for the second Azure AI agent
        bad_agent = AzureAIAgent(
            client=client,
            definition=bad_agent_definition,
        )

        # 5. Place the agents in a group chat with a custom termination strategy
        chat = AgentGroupChat(
            agents=[good_agent, bad_agent], # Change which agents are checked for termination criteria - including the bad agent here will cause it to terminate the chat early
            termination_strategy=ApprovalTerminationStrategy(agents=[good_agent, bad_agent], maximum_iterations=10),
        )

        try:
            # 6. Add the task as a message to the group chat
            await chat.add_chat_message(message=TASK)
            print(f"# {AuthorRole.USER}: '{TASK}'")
            # 7. Invoke the chat
            async for content in chat.invoke():
                print(f"# {content.name or '*'}: '{content.content}'\n")
        finally:
            # 8. Cleanup: Delete the agents
            await chat.reset()
            await client.agents.delete_agent(good_agent.id)
            await client.agents.delete_agent(bad_agent.id)


if __name__ == "__main__":
    asyncio.run(main())