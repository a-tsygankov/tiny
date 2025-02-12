In this round, we will ask you to write a small application as proof of concept for a TinyURL-style service.

*** We do not need to see an actual persistent storage layer. Please do NOT use EntityFramework (even in-memory), SQLite, Redis, or similar. ***

TinyURL is a service where users can create short links, such as tinyurl.com/3rp36a3s, redirecting to longer links, such as https://www.adroit-tt.com. Please do NOT use the actual TinyURL or any other URL shortening service.

We would like you to include a small web UI to interact with a mockup tiny URL service (do not use the actual TinyURL or any other pass-through service.) The service (back-end part of the assignment) should be written in C#; for the web UI, please use React. We do not want auto-generated (i.e., swagger) web code. Similarly, we do not need to see an actual persistent storage layer. Feel free to mock this out in memory however you best see fit. Lastly, note that a single long URL might map to a few short URLs.

Although this is a POC, we would still like to see it designed with architecture in mind. To this end, please consider your schema, service methods, and constraints accordingly. The web service should safely handle concurrent requests from multiple users; however, each user is anonymous without a separate individual account. 

The POC should support:

Creating and Deleting short URLs with associated long URLs.
Getting the long URL from a short URL.
Getting statistics on the number of times a short URL has been "clicked," i.e., the number of times its long URL has been retrieved.
Entering a custom short URL or letting the app randomly generate one while maintaining the uniqueness of short URLs.
 

Importantly, the service should be a working solution with a project we can run (not a Word document.)

 

Once you've completed the assessment, please zip up your project and resubmit it to this email chain. Title your project with the format: "FirstName-LastName-Adroit." Please do NOT include meta files or compiled binaries - in other words, please do not send bin/, obj/, or .vs/ folders. If .zip doesn't get through, please send us a link via Google Drive, Dropbox, or any other file-sharing service. You have 48 hours to submit your solution.

 