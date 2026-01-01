---
title: _Chirp!_ Project Report
subtitle: ITU BDSA 2025 Group 20
author:
  - "Gustav Nilsson <gnil@itu.dk>"
  - "Kian Frede Frølund <kiaf@itu.dk>"
  - "Patrick Fransgaard <pafr@itu.dk>"
  - "Oskar Snoer Kristiansen <oskk@itu.dk>"
numbersections: true
---

# _Design and Architecture of Chirp!_

## Domain model

![Domain model diagram of the models](diagrams/Domain_Model_Diagram.png)

The Domain model illustrate our Models. We chose to illustrate the ASP.NET Identity in an abstract relation to the Author. It represents the authenticated user, while Author represents the user domain.

## Architecture - In the small

For the Chirp! program we have implemented the Onion Architecture structure. The structure is built up in layers: Core, Infrastructure, and Web.

The Core layer forms the center of the Onion and is independent of all other layers. The Core contains the service interfaces that define the behavior of the application. This layer does not depend on any of the other layers.

The Infrastructure layer surrounds the Core and provides implementations of the behavior defined in the Core layer. This includes repository implementations, the database and external services, such as CheepService.cs. The Infrastructure layer depends on the Core layer, but does not depend on the Web layer.

The Web layer is the outer layer of our Onion and is responsible for handling HTTP requests and returning responses to the client. All the request/response logic can be found here. It depends on the Core layer and uses the Infrastructure layer through dependency injection.

The layers use Data Transfer Objects (DTOs) to communicate and transfer data from the center of the onion and outward, ensuring that entities are not exposed outside the Core layer.

![Diagram illustrating the onion architecture layers and their dependencies](diagrams/Architecture_In_the_small.png)

## Achitecture of the deployed application

Chirp! is a client-server web application built with ASP.NET Core Razor Pages.

- **Server**: Single ASP.NET Core web application (Chirp.Web project) deployed to **Azure App Service** (Linux plan).
- **Database**: SQLite database stored in project and accessed by the application.
- **Client**: Standard web browser communicating with the server exclusively over HTTPS.
- Static assets (CSS, JS, images) are served directly from the Azure App Service.

No separate API layer or SPA client — the Razor Pages engine handles both server-side rendering and minimal dynamic behavior.

![Deployment diagram using components to illustrate main concept](diagrams/Deployment_Diagram.png)

## User activities

We illustrate two main user journeys to show how functionality differs based on authentication status.

_Unauthenticated (public) user:_

1. Arrives at the landing page (public timeline) → sees the global timeline of the most recent public cheeps.
2. Can navigate to any author's profile page (`/@username`) → reads that author's cheeps.
3. Cannot post cheeps, follow authors, or access personalized features.

_Authenticated user:_

1. Logs in via GitHub OAuth (using ASP.NET Core Identity) at login.
2. After login → redirected to public timeline showing latest cheeps.
3. From there they can access My Timeline to see own and followed authors cheeps.
4. Can submit new cheeps via the form on the timeline page.
5. Can follow/unfollow other authors from any timeline or from About Me page.
6. Can view list of authors they follow from about me page.

![Flowchart showing unauthenticated vs authenticated user journeys](diagrams/User_Activities.png)

## Sequence of functionality/calls through Chirp!

![UML sequence diagram of the main page load flow](diagrams/Sequence_of_functionality.png)

# _Process_

## Build, test, release and deployment

![Activity diagram displaying the two workflows for deployment and publishing](diagrams/Activity_Diagram.png)

For our project we used two workflows. One for publishing and releasing the latest version and one for deployment on our Azure Web App. Both apply a lot of the same logic.
The publish workflow attempts to build before anything else. If that fails the workflow stops. Then it applies the tests followed by a published version of the program in GitHub.
The deployment does the same first steps. Build followed by tests, then it logs into Azure and deploys to the web application.

We activate the deployment workflow by pushing anything to the main branch. To activate the publish workflow we manually release a version through git.

A better way would have been to have a combined workflow that did Build, test, release and deployment on every push to the main branch.

## Team work

We did not use any form of project boards, we instead used the sessions as a sort of benchmark, where we were in the project timeline and what our next goal was. We communicated a lot with each other and had a lot of in-person coding sessions. That is why we did not comment on pull requests. We went through them in-person instead and talked about what was good and what could be improved. In hindsight, writing this down would have been better regardless, but hindsight is 20/20.
We had one feature we never got around to implementing, displaying usernames instead of users’ email addresses. We deprioritized it because it was not a critical feature, and in the end, we did not get to making it.

![Diagram of how we make issues on git](diagrams/Team_Work.png)

We would discover a problem or a feature that needed to be added. Then make an issue on git while talking about what to do to fix or make the feature. We would then make the feature and then merge it into main.
We tried to do trunk-based development in the beginning, but shifted to feature-based development, as we wanted Main to be working and not a mess of half-written code.

As a sidenote kiaf and Xerzes01 is the same person, as well as PipFuglen and oskk.

## How to make Chirp! work locally

1. Clone repository: "git clone https://github.com/ITU-BDSA2025-GROUP20/Chirp.git"
2. Make sure dependencies are installed. Navigate to the Chirp folder and run: "dotnet restore"
3. Run: "cd src/Chirp.Web" this moves you within the application layer.
4. Run these two commands:

- dotnet user-secrets set "authentication:github:clientId" "Ov23liTQgaA8DxRmyvxf"
- dotnet user-secrets set "authentication:github:clientSecret" "8add950ef7c1c7f53a5b482787630bdfa0eb0da1"
  This configures user-secrets for GitHub OAuth.

5. Followed by: "dotnet watch". This starts a local server at "http://localhost:5273". The "dotnet watch" command should take you straight to the localhost.
6. Now you are free to interact with the application!

## How to run test suite locally

We assume you are at the last step of “How to make Chirp! work locally”

1. Press “Ctrl + C”
2. Run “cd ..” until you are at the root
3. Run “cd test”
4. Then run “dotnet test”. This will start the tests.

# Ethics

## License

We use MIT License.

## LLMs, ChatGPT, CoPolit and others

We have used ChatGPT, CoPilot, Venice.ai, and Grok. If used correctly, the chatbots can be used to speed up the process of coding, something like changing all of one variable to another, but if used to just make code from scratch, you lose the learning aspect of it and just get the answer. We have used chatbots after searching the internet or talking with each other, and even when using them, we asked how it got the answer. We believe that learning is important, and if you just use a chatbot to get all the answers, you don't learn.
