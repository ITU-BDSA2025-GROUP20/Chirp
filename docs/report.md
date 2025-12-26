Design and Architecture of Chirp!

Domain model
Insert UML class diagram.


Architecture - In the small
Make illustration of the Onion and what resides inside it.

For the Chirp! program we have implemented the Onion-Architecture structure. The structure is built up in layers: Core, Infrastructure and Web. The Core layer being completely independent of the other layers. The Infrastructure layer does not depend on the web application layer either. 
The layers use Data Transfer Objects (DTO's) to communicate and transfer data from the center of the onion and outwards.

Achitecture of the deployed application
Illustrate server-client application

User activities

We illustrate two main user journeys to show how functionality differs based on authentication status.

**Unauthenticated (public) user:**
1. Arrives at the landing page (public timeline) → sees the global timeline of the most recent public cheeps.
2. Can navigate to any author's profile page (`/@username`) → reads that author's cheeps.
3. Cannot post cheeps, follow authors, or access personalized features.

**Authenticated user:**
1. Logs in via GitHub OAuth (using ASP.NET Core Identity) at login.
2. After login → redirected to public timeline showing latest cheeps.
3. From there they can acces my timeline to see own and followed authors cheeps.
4. Can submit new cheeps via the form on the timeline page.
5. Can follow/unfollow other authors from any timeline or from about me page.
6. Can view list of authors they follow from about me page.

Sequence of functionality/calls through Chirp!



Process

Build, test, release and deployment

Team work

How to make Chirp! work locally
explain in detail how to make it work locally.

How to run test suite locally
Same here. explain in detail.

Ethics

License

LLMs, ChatGPT, CoPolit
