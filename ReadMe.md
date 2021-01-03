# Introduction
MemCheck is a flashcard web site, a tool to help you know things by heart.

# Archi, specification doc, resources
- The Application project defines *what* we want to display, not how we display it. So it returns raw data, not transformed for display.
- The UI project defines *how* we display the data sent by the Application. *The UI project is not allowed to see the model*, it only sees the Application.
- In the Web UI, the html pages never show data as returned by the Application, but as returned by the controllers. It is the job of the controllers to adapt the data for correct display.

## Decks
- By design, a card may not be twice in a deck

## Naming
- A card in the _Unknown heap_ is a card to _Learn_. A card in the another heap is a card to _Repeat_.

## Notes for doc
- The DB schema prevents deletion of a card used in a deck
- About confidentiality of contents, note that moderators will be able to access data to check that no unbearable content is added (such as porn), even though a card is marked as private.
- Les labels sont des indicateurs pour vous aider dans vos recherches. Ils ne garantissent pas une qualité ou une continuité de style entre cartes. Les combinaisons de critères de recherche sont souvent le bon moyen pour trouver le lot de cartes qui vous conviendra pour un sujet donné (par exemple les cartes de telle personne avec tel label).

### Comment débuter

#### Choisir des cartes pour découvrir MemCheck
- MemCheck contient beaucoup de cartes sur des sujets très variés. Parcourir la liste des labels peut vous aider à vous faire une idée. Par exemple, si vous décidez d'apprendre le découpage administratif de la France en régions et départements, un lot de cartes existe. Une des façons de sélectionner ces cartes est de faire une recherche sur le label "xxx". Cette recherche liste beaucoup de cartes. Vous choisissez lesquelles vous voulez (on peut par exemple souhaiter savoir localiser une région donnée, mais pas lister ses départements).


## Display
- On my phone, in portrait orientation, window.innerwidth = 424. In landscape, window.innerwidth = 809.

## Azure
- L'_App service plan_ doit être sous Windows pour que le l'_App service_ y soit aussi et qu'on puisse utiliser du web deploy depuis VS.
- The web site is deployed in an Azure App Service (aka a _web app_)
- Après création de la web app, pour obtenir toutes les infos de publication, dans l'overview de la web app, clicker sur _Get publish profile_, en haut. Il faut importer ça dans VS pour créer un profil de publication.
- The db is deployed as an Azure SQL Database

## Tags
- Would you consider tag hierarchizing ? This would be convenient for such categories as "English vocabulary/Cooking". No. Tagging is in fact the sheer opposite of hierarchizing, allowing all combinations. Hierarchies quickly become a nightmare: why not Cooking/Vocabulary instead ?

## Unit tests
- My initial plan was to use an in-memory database for unit tests (see `UserCardDeletionsNotifierTests.OptionsForNewDB`). Unfortunately, I discovered that this is not at all a good substitute to mimic the prod db. For example:
  - It will accept null values for non-nullable fields, where SQL Server throws an exception.
  - Some foreign key constraints can be violated without failure.
  - Cascade delete behaves differently.

# In progress

# Bugs

# To do, at little cost
- Effacement image si pas utilisée. Nécessite d'abord recherche avec une image donnée pour pouvoir remplacer, et affichage "Utilisée dans n pages" avec lien
- Prevent modification of a deck with no or too long description, or duplicated descriptions for the same user (reuse what was done in create deck, without forgetting to check ownership)
- Upon creating a new version of a card, warn that this will impact n users who have it in a deck.
- The mail sent by notifier should include an history link and a diff link
- Fix the hyperlinks in the doc pages

# To do at medium cost
- Mettre l'envoi de mail dans une Azure function ? Permet de faire ça en asynchrone ? Et pourquoi pas tout faire en Azuere function ? Eg, le rating par un utilisateur. Par exemple en utilisant un blob trigger, et le blob est ce qu'il faut traiter. Il y a plein de triggers différents (un autre exemple est le queue trigger).
- De même que j'ai optimisé GetCardsToLearn qui faisait trop de Include, voir si on peut faire pareil pour ce qu'affiche la page de démarrage.
- Implémenter le diff de versions d'images (metadata)
- Implémenter le restore de versions d'images (metadata)
- Revoir le critère de recherche de cartes "Propriétaire". On peut imaginer un critère "Utilisateur contributeur", au sens utilisateur créateur d'au moins une version de la carte. Voir s'il faut mentionner dans la GUI un coût en perf.
- Supporter les gifs animés : https://commons.m.wikimedia.org/wiki/File:Cardinal_W_Q.gif (cardinale ouest)
- User reputation. Public reputation is the average of this user's public cards evaluation. Private reputation is the same, but for private cards only (this can be useful for working on cards before making them public). Note that there is a problem with speaking about _user's public cards_: there is no such thing as a _owner_ of a card. So, are we speaking of a card a user has created a version of?
- Rename _CardNotificationSubscription_ to _CardSubscription_, and _MemCheckDbContext.CardNotifications_ to _CardSubscriptions_. Watchout: we don't want to lose any data.
- Use Azure functions to run Notifier on cron intervals. This is not implemented yet because Azure functions don't support .NET 5 yet (should be available by Jan 2021).
- Check by unit test that the documentation contains no broken hyperlink (probably needs to render it)

# To do at big cost
- Card authoring: Have the user review his changes before saving a new version of a resource (card, tag, deck)
- Review security on all verbs of the application. For example, if someone calls the delete deck page with a deck he does not own as argument, check that it fails. Create an abstract ancestor Verb<TRequest, TResponse>, with a sealed method Run, which begins with checking the request's validity, and returns a TResponse. A question not so easy is: should Run be async? Securize the application: check that the user has the rights for an operation on application side. Be sure that the user is actually authentified. Use a token security system? For example, in MoveCardToHeap, we should check that the user is the owner of the deck
- search page criteria. All criteria should adapt to the selected deck if it is inclusive (eg if in this deck there are only cards with owner A and B, don't offer filtering on owner = c)
  - card rating
	- card language
	- creation date
  - last version date
  - expiry date
	- author (including "Your cards"). This searches for a card which has this user as author of any version? Implémenter le filtrage sur le créateur de version dans la page de recherche. Garde comme nom auteur, et ça veut dire auteur d'une version. Puis implementer filteringOnOwnerCurrentUser, en faisant gaffe à la redondance avec _visibilité privée_. Supporter _Owner of current version_ et _Owner of any version_.
  - Revoir le filtre de visibilité dans la recherche de carte : Ignorer / Privée / Visibilité restreinte / Visibilité publique (dans la Doc : Visibilité restreinte signifie que la carte n'est pas publique ; il peut s'agir d'une carte complètement privée pour vous, ou d'une carte qu'un utilisateur vous partage, ou que vous partagez avec un ou des utilisateurs). La privée ne doit pas être accessible si on choisit un autre utilisateur comme auteur d'une version
- Support sorting in the search view
- Quand on passe d'une recherche d'image au mode édition, la return url pourrait contenir un object code (comme ce qui est fait dans l'authentication) pour que les critères de recherche ne soient pas perdus au retour. Puis faire de même pour les autres recherche.
- To improve the speed of loading the search page, the getting of static data and the first runquery could be run in parallel?
- Do we want to support a user's default deck, which is selected by default in the various pages?
- Support other media types? Video, sound
- Personal notes about a card : a user may associate personal textual info, private, about any card he can access (eg card says something, but user wants to write But my mother used to say xxx)
- Offer the possibility to create a tag in place (in the authoring view). To be done by implementing a tag Vue component. The component deals with displaying the cloud of tags, and the combo box if the list is modifiable. Decide if the same component is used for users with view. The code for using tags in the GUI is duplicated between search, authoring and learn (both in JavaScript and Html).
- Use ValidateAntiForgeryToken
- En fin de modification d'une carte, recharger la carte (pour le cas où on continue à la modifier) ? If decide not to do that, switch the page to new card creation mode.
- If we implement a card view mode, which is not editing, on end of edit we could switch to this mode for the edited card.
- How to assign the admin role? Use the app settings in Azure? See class MemCheckUserRole. What actions to reserve to admin only (eg create language, rename tag)?
- Forbid two languages with same name.
- Check Backup method. Export database as json? Trouver où sont les backups dans Azure
- Tags garbage collector: delete a tag not used by any card for more than a month
- Support tags on media?
- Create metrics for each function of the application project, for perf & feature tracking
- Since a tag doesn't have a owner, we currently have no info about who created one. That's bad. Find the good way to answer this problem. For the card language, I think we want to allow creating one for admin only. Since tags have now owner, some of their actions are reserved to admin?
- Home page: In the multi deck display, the links to the learning pages should select the appropriate deck
- Tag family: should we support a tag which includes other ones? Eg histoire de France could include R�volution fran�aise, Premier empire, Charles de Gaulle). Or use a dot as separator? Eg maths would include maths.college and maths.lycée, and we could have Histoire.Histoire de France.Période napoléonienne
- Should we check that the user is allowed to see a card each time we load one for this user ? (And update the DB if we find a discrepancy)
- ABout perf, review this page, and study ResponseCompression: https://www.syncfusion.com/blogs/post/10-performance-improvement-tips-for-asp-net-core-3-0-applications.aspx. https://docs.microsoft.com/fr-fr/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.1
- Introduire une notion de leçon ? Par exemple Régions, départements et préfectures Françaises. Départements dans région. Région de département. Nombre de départements dans région. Préfectures
- Make MemCheck.Application a separate service (an API running as a standalone project). Will allow even better decoupling.
  - The GUI (the web site) should not see the domain, but only talk with the application.
  - The GUI (the web site) should not see the database, but only talk with the application.
  - Needs that interactions with the application are authentified. This will need investigation of authentification tokens, or something like this.
- Use an emulator to check usability of site on iPHone. Drop downs may not be a good idea.
- Review Site.js : where is this used? How often is it used? What is the impact on perf?
- Use specific resource files for display on small screen, to have smaller texts. Eg replace menu entry texts with icons. See how I used the css to switch between _display: inline-block_ and _display: none_ according to the screen size in the page \Areas\Identity\Pages\Account\Manage\Subscriptions.cshtml
- Forbid uploading an image in an image in the DB already has the same blob (implement a crc, hash of the blob?)
- Make the app work even if changes are made simultaneously in various sessions. We're mostly ok. The authoring window is wrong in saving the whole card. It should only update the fields the user has changed, so that for example if someone has added tags in another window, this info is not lost.
- Limit length of user name to reduce the risk of display glitches. Something like 30 to 50 chars sounds like a good limit.
- See what we need to do about RGPD, data ownership (a cookie might be needed)
- Multilingual cards: a card can have a link to a card in another language, meaning that it is its version in this language. This is not related to a version, but to the card.
- A user should be allowed to download all his cards (after all, this is his data)
- User option to not present the same not known card to learn in 24h : Stash learning failure for 24 hours in the discard pile. (in french: _Carte coincée dans la pioche_)
- Do something about orphan cards: cards whose only user with view is a deleted account (ie previous cards). Delete them? (by making them Previous)
- Statistics page. Option to filter on cards the user has created a version of. See : nb decks containing a card. Average evaluation of card. Nb versions of card. Age of card. Nb versions of card. Nb users who have created versions of the card
- Ignorer les accents dans les recherches (cartes, images, labels). Peut-être qu'il suffit d'utiliser InvariantCultureIgnoreCase. Idéalement la recherche de "Que signifie bâbord" devrait trouver "Que signifie _bâbord_". ça va dans le sens d'avoir un vrai moteur de recherche. Chercher dans le texte après rendering markdown ne convient pas, parce que ce texte-là contient d'autres balises (eg les <strong>).
