## 🧑‍🍳 High Level Architecture 🧑‍🍳

Provides a birds eye view of the entire architecture of Culinary Command. The main components include:
- EC2 Auto Scaling Group
- MCP Server hosted on a lambda


### 💻 Web Application Diagram 💻

Goes over the EC2 Auto Scaling Group architecture

- The Application Load Balancer (ALB) is responsible for redirecting traffic to an EC2 Instance.

- Each EC2 Instance hosts a version of the Culinary Command app. We ensure each EC2 Instance hosts the same app by using a launch template.

- We can define a launch template that our Auto Scaling Group references as a blueprint for each EC2 instance.

- An Auto Scaling Group determines the configuration of the deployed EC2 instances. For example, we can say we want a minimum of 2 EC2 instances deployed at all times and a maximum of 4 EC2 instances deployed at once.

- A Target Group tells the Application Load Balancer where to forward traffic.

- The EC2 Security Group will serve as a basic firewall. e.g. allow inbound traffic from SSH (22), HTTP (80), and HTTPS (443) and allow outbound traffic to anywhere

### 🤖 SmartTask MCP Server Diagram 🤖

Gives a high level view of the MCP server architecture used for SmartTasks.

- Users interact with the `culinary-command.com` web app and events are sent to the Lambda hosted MCP server using AWS SigV4.
- The MCP server exposes a list of MCP tools that the GenAI model can leverage to make calculated decisions for creating SmartTasks.
- The GenAI model can invoke `generate_plan()` that orchestrates everything. It eventually creates a list of tasks and assigns to users based on the algorithm.

### 👨‍💻 Developer Workflow Diagram 👨‍💻

Represents the workflow that we have been working with throughout the semester.

- We host our code on GitHub and use `GitHub Actions` as our CI / CD pipeline to deploy our infrastructure.
- The CI / CD pipeline is responsible for making updates to the `EC2 Auto Scaling Group` and `Lambda hosted MCP server`.