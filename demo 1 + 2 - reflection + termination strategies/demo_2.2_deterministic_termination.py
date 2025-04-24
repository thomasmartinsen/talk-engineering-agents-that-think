import asyncio

from typing import Annotated

from azure.identity.aio import DefaultAzureCredential

from semantic_kernel.agents import AgentGroupChat, AzureAIAgent, AzureAIAgentSettings
from semantic_kernel.agents.strategies import TerminationStrategy
from semantic_kernel.contents import AuthorRole
from semantic_kernel.functions import kernel_function


class ExperimentationPlugin:
    """A plugin for finding prime numbers."""

    @kernel_function(description="find prime")
    async def find_prime(self, n: Annotated[int, "The number to check"]):
        """Find a prime number."""
        if n < 2:
            return False
        for i in range(2, int(n**0.5) + 1):
            if n % i == 0:
                return False
        return True
    
    @kernel_function(description="append number to log")
    async def append_log(self, content: Annotated[int, "The number to log"]):
        """Append content to the experiment log."""
        with open("experiment.log", "a") as log_file:
            log_file.write(f"{content}\n")


class ApprovalTerminationStrategy(TerminationStrategy):
    async def should_agent_terminate(self, agent, history):
        """Check if the agent should terminate."""
        try:
            with open("experiment.log", "r") as log_file:
                lines = log_file.readlines()
                # If the file has 6 or more lines, return True
                print(f"\n# Experiment log: {lines}")
                return len(lines) >= 6
        except FileNotFoundError:
            # If the file does not exist, return False
            return False


GOOD_AGENT_NAME = "GoodProfessor"
GOOD_AGENT_INSTRUCTIONS = """
You are an old-school math professor.
The goal is to find the solution to the task.
Focus on the task. Don't waste time with chit-chat.
Suggest no more than 3 numbers at a time.
"""

BAD_AGENT_NAME = "BadProfessor"
BAD_AGENT_INSTRUCTIONS = """
You are a bad professor that is confidently suggesting wrong answers.
Focus on the task. Don't waste time with chit-chat.
Suggest no more than 3 numbers at a time.
"""

ASSISTANT_AGENT_NAME = "Assistant"
ASSISTANT_AGENT_INSTRUCTIONS = """
You are an assistant that is helping the professors to find the solution to the task.
Always use your tools to verify the suggestions of the professors. Don't ask for permission.
Log every verified number to a file and state which number was correct.
Don't waste time with chit-chat, just verify the suggested numbers. Make sure to check every suggestion!
"""


TASK = "Keep suggesting new prime numbers"


async def main():
    ai_agent_settings = AzureAIAgentSettings()

    async with (
        DefaultAzureCredential() as creds,
        AzureAIAgent.create_client(credential=creds) as client,
    ):
        good_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=GOOD_AGENT_NAME,
            instructions=GOOD_AGENT_INSTRUCTIONS,
        )

        good_agent = AzureAIAgent(
            client=client,
            definition=good_agent_definition,
        )

        bad_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=BAD_AGENT_NAME,
            instructions=BAD_AGENT_INSTRUCTIONS,
        )

        bad_agent = AzureAIAgent(
            client=client,
            definition=bad_agent_definition,
        )

        assistant_agent_definition = await client.agents.create_agent(
            model=ai_agent_settings.model_deployment_name,
            name=ASSISTANT_AGENT_NAME,
            instructions=ASSISTANT_AGENT_INSTRUCTIONS,
        )

        assistant_agent = AzureAIAgent(
            client=client,
            definition=assistant_agent_definition,
            plugins=[ExperimentationPlugin()],
        )

        chat = AgentGroupChat(
            agents=[good_agent, bad_agent, assistant_agent],
            termination_strategy=ApprovalTerminationStrategy(agents=[assistant_agent], maximum_iterations=10),
        )

        try:
            await chat.add_chat_message(message=TASK)
            print(f"# {AuthorRole.USER}: '{TASK}'")
            async for content in chat.invoke():
                print(f"\n# {content.name or '*'}: '{content.content}'")
        finally:
            print("\n")

            await chat.reset()
            await client.agents.delete_agent(good_agent.id)
            await client.agents.delete_agent(bad_agent.id)

            # Delete experiment log file
            try:
                import os
                os.remove("experiment.log")
            except FileNotFoundError:
                pass


if __name__ == "__main__":
    asyncio.run(main())