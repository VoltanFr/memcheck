﻿<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css">

# Bienvenue !
MemCheck est un logiciel d'auto-apprentissage, qui vous aide à apprendre par cœur ce que vous voulez, comme le vocabulaire d'une langue étrangère, des capitales, des dates importantes, des formules mathématiques, etc.  
MemCheck est basé sur le principe des cartes d'apprentissage : la face avant d'une carte indique une question, et la face arrière contient la réponse. Un apprentissage consiste à regarder la question, formuler pour soi-même une réponse, regarder la réponse, et décider si on avait bon. Et bien sûr MemCheck gère pour vous les intervalles entre les vérifications de connaissances, de façon croissante au fur et à mesure des répétitions.  
Wikipédia a une [page dédiée aux cartes d'apprentissage](https://fr.wikipedia.org/wiki/Carte_m%C3%A9moire_(apprentissage)).

## Choisissez ce que vous voulez apprendre parmi les cartes existantes
Vous pouvez **sélectionner des choses à apprendre parmi la base de données des cartes disponibles**.
La recherche se fait dans le menu [Cartes/Parcourir](https://memcheckfr.azurewebsites.net/Search/Index).  
Dépliez la zone des critères de sélection par le menu à trois points <i class="fa fa-ellipsis-v"></i>.  
Par exemple, si voulez apprendre à identifier les régions françaises, spécifiez comme [label](https://memcheckfr.azurewebsites.net/Tags/Index) requis `Régions françaises`.  
Un autre critère de sélection particulièrement intéressant est l'évaluation moyenne des utilisateurs (qui va de une à cinq étoiles).  
Dans la liste, vous pouvez cocher des cartes, puis les ajouter à votre paquet en utilisant le bouton en forme de paquet en haut à gauche <i class="fa fa-inbox"></i>.  
Pour en savoir plus sur la recherche de cartes, lisez la [documentation spécifique](https://memcheckfr.azurewebsites.net/Doc/MdRenderer?refererRoute=%2FSearch%2FIndex&cultureName=fr).

## Créez vos cartes
Il est très facile d'écrire vos propres carte : ça se passe dans la [page dédiée](https://memcheckfr.azurewebsites.net/Authoring/Index).  
Vous contrôlez la visibilité des cartes que vous créez (_strictement privées_, _visibles seulement par des utilisateurs choisis_, _publiques_).

## L'apprentissage
Votre sélection de cartes s'appelle un [paquet de cartes](https://memcheckfr.azurewebsites.net/Decks/Index). Dans un paquet, les cartes sont réparties par **tas** selon l'état de vos connaissances.  
Lorsque vous ajoutez une carte dans votre paquet, elle est dans le tas nommé _cartes non apprises_.  
Lorsque vous apprenez une carte, si vous indiquez que vous connaissiez la réponse, la carte est déplacée dans le tas supérieur, et si vous aviez faux, la carte est remise dans le tas des non apprises.  
Chaque tas a sa période d'expiration, dont la formule est deux élevé à la puissance le numéro du tas (2<sup>tas</sup>). Ainsi, une carte qui est dans le tas numéro vous sera soumise à nouveau deux jours après qu'elle y soit arrivée. Dans le tas numéro deux, cet intervalle est de quatre jours, etc. Une carte qui est dans le tas numéro 10 ne vous sera présentée à nouveau qu'au bout de 2 ans et 9 mois !  
**Un aspect majeur est que _vous_ indiquez si vous connaissiez la réponse, à votre guise**. Par exemple, si une fiche de la catégorie Science physique vous demande [la densité de l'or](https://memcheckfr.azurewebsites.net/Authoring?CardId=534b3214-5880-47a0-d8f0-08d7eba1e1a5), c'est vous qui validez ou non votre réponse.

## Gratuit, collaboratif, évolutif
MemCheck est une solution coopérative : les cartes publiques peuvent être modifiées par tous les utilisateurs, dans le but d'amélioration continue.  
Pas de panique : quand quelqu'un modifie une carte, il s'agit d'une nouvelle version et l'historique des versions est conservé, permettant de ne pas perdre d'information.  
Vous pouvez _suivre_ des cartes, et recevoir des notifications quand elles sont modifiées.  
À propos de la création de cartes publiques, nous vous recommandons la lecture de la page [Créer des cartes de qualité](https://memcheckfr.azurewebsites.net/Doc/MdRenderer?refererRoute=%2FAuthoring%2FIndex&cultureName=fr).  
Le logiciel MemCheck lui-même est ouvert, vous pouvez consulter son code source ou apporter des améliorations sur la [page dédiée du site GitHub](https://github.com/VoltanFr/memcheck).