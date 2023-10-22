# Wirtualny Słoń
Wirtualny Słoń is an early-warning system for children with depression.![394550517_294858706826777_4830754478342229400_n](https://github.com/adamkulik/guardian/assets/13134704/d29cc003-f1b9-4395-af6f-f33329ab09a7)

Wirtualny Słoń works as an HTTPS proxy that decrypts HTTPS traffic using FiddlerCore. It then scans if HTTP request is a search engine query,
and if yes, it tries to extract the query being searched and runs it through IBM's watsonX.AI component to perform a sentiment analysis.

![wirtualny słoń](https://github.com/adamkulik/guardian/assets/13134704/1448596c-ec26-4216-a58c-53b481270727)
The result of the sentiment analysis is then used to check if the search query might be correlated with:
+ panic attack
+ depression
+ mental exhaustion
+ fears
+ attention deficit
+ loneliness 
+ problem with peers
+ suicide
+ bullying
+ cyberbullying
+ suicidal thoughts
+ abuse
+ addictions

If such search query is detected, an XSS injection is performed on server response to embed a script with chatbot window. The chatbot either offers a direct contact to the therapist or proposes steps to solve the issue.

The alerts are also logged and can be later sent to therapist to have a note on how the child is managing his mental health.

![394716514_711487200849095_5299568306155238494_n](https://github.com/adamkulik/guardian/assets/13134704/25474c30-129b-4130-9f41-0c144a6b8399)
![394739626_1412460069338931_898863309783053931_n](https://github.com/adamkulik/guardian/assets/13134704/07b1eb3f-58fa-4837-91e5-ac1af480ac1e)

For future ideas, we plan to expand the app with features like:
+ looking through social media chats (facebook, instagram, discord)
+ adding polish language
+ more detailed chat bot
+ watch over comments on all kind of websites
+ added support for mobile devices
+ Web UI for browsing patients search results
