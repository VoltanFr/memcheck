var app = new Vue({
    el: '#SearchRootDiv',
    data: {
        mountFinished: false,
        singleDeckDisplayOptimization: false,
        allStaticData: "",   //SearchController.GetAllStaticDataViewModel - We never mute this field after mounted has finished
        possibleDecks: [],   //SearchController.GetAllStaticDataDeckViewModel - This array changes according to the selected deck. It contains in first position a fake deck named "Not filtering" with Guid zero.
        selectedDeck: "",  //SearchController.GetAllStaticDataDeckViewModel - See selectedDeckIsForInclusion
        possibleTargetDecksForAdd: [], //SearchController.GetAllStaticDataDeckForAddViewModel
        possibleDecksInclusionChoices: [],   //{selectedDeckIsInclusive: bool, choiceText: string}. This field only makes sense when selectedDeck is not null. Tells if the query will report the cards of selectedDeckor exclude the cards of the selected deck
        deckSelectionIsInclusive: true,
        possibleHeaps: [],  //SearchController.GetAllStaticDataHeapViewModel. Available only when filtering inclusive on a deck, this shows the heaps which exist in the deck. There is a first heap named "ignore" with id -1.
        selectedHeap: "",  //int. Available only when filtering inclusive on a deck, the heap in which all cards must be. -1 means no filtering on heap
        addToDeckDatePossibleChoices: [],  //{choiceId: int, choiceText: string}. Only when filtering inclusive on a deck. 0 = ignore date on which cards were added to the deck, 1 = before the selected date, 2 = on the day, 3 = after
        addToDeckDate: "",  //Date. Available only when filtering inclusive on a deck, date considered for filtering on date on which the card was added to the deck
        selectedAddToDeckDateChoice: 0, //choiceId: int
        expiryDatePossibleChoices: [],  //{choiceId: int, choiceText: string}. Only when filtering inclusive on a deck. 0 = ignore date on which cards expire, 1 = expiry before the selected date, 2 = on the day, 3 = after
        expiryDate: "",  //Date. Available only when filtering inclusive on a deck, date considered for filtering on date on which the card expires
        selectedExpiryDateChoice: 0, //choiceId: int
        possibleRequiredTags: [], /* SearchController.GetAllStaticDataTagViewModel - The tags we *currently* display in the required tags combo box. 
                            * Depending on the choices on the deck, this can be all the tags of the database, or all the tags which appear in selectedDeck.
                            * Contains in first position a fake tag named "Not filtering" which has for effect the emptying of selectedRequiredTags */
        selectedRequiredTags: [],   //SearchController.GetAllStaticDataTagViewModel
        selectedRequiredTagToAdd: "",   //SearchController.GetAllStaticDataTagViewModel. Model of the combo box, used to manage adding
        possibleExcludedTags: [], /* SearchController.GetAllStaticDataTagViewModel - The tags we *currently* display in the excluded tags combo box. 
                            * Depending on the choices on the deck, this can be all the tags of the database, or all the tags which appear in selectedDeck.
                            * Contains in first position a fake tag named "Not filtering" which has for effect the emptying of selectedExcludedTags
                            * And in second position a fake tag named "All": when the user selects this fake tag, it is pushed alone in selectedExcludedTags */
        selectedExcludedTags: [],   //SearchController.GetAllStaticDataTagViewModel
        selectedExcludedTagToAdd: "",   //SearchController.GetAllStaticDataTagViewModel. Model of the combo box, used to manage adding
        possibleOwners: [],  //SearchController.GetAllStaticDataUserViewModel. Contains in first position a fake deck named "Ignore" with Guid zero.
        selectedOwner: "",  //Guid
        visibilityFilteringPossibleChoices: [], //{choiceId: int, choiceText: string}. 1 = ignore this criteria, 2 = cards which can be seen by more than their owner, 3 = cards visible to their owner only
        selectedVisibilityFilteringChoice: 1,  //choiceId: int
        possibleRatingFilteringKind: [],   //{choiceId: int, choiceText: string}. 1 = ignore this criteria, 2 = cards with the selected rating and above, 3 = cards with the selected rating and below
        selectedAverageRatingFilteringKind: 1,
        possibleRatingFilteringValues: [],  //{choiceId: int, 1 to 5, choiceText: string}
        selectedAverageRatingFilteringValue: 5, //1 to 5
        selectedNotificationFilteringId: 1,  //choiceId: int
        possibleSelectedNotificationFiltering: [],   //{choiceId: int, choiceText: string}. 1 = ignore this criteria, 2 = cards registered for notification, 3 = cards not registered for notification
        textFilter: "", //string
        pageNo: 1, //int. First page is number 1
        pageSize: 50,   //int
        offeredPageSizes: [10, 25, 50, 100, 500],
        runResult: {
            pageCount: 0,
            totalNbCard: 0,
            cardsWithSelectionInfo: [], //{selected: bool, card: result of cardFromControllerResult}
        },
        guidNoCurrentUser: '00000000-0000-0000-0000-000000000000',
        guidNoDeckFiltering: '00000000-0000-0000-0000-000000000000',
        guidNoTagFiltering: '00000000-0000-0000-0000-000000000000',
        guidExcludeAllTagFiltering: '11111111-1111-1111-1111-111111111111',
        guidNoUserFiltering: '00000000-0000-0000-0000-000000000000',
        loadingQuery: false,
        addTagDropDownButtonDisabledReason: "", //string
        possibleHeapsForMove: [],  //SearchController.GetAllStaticDataHeapViewModel
        mountDebugInfo: "",
        lastQueryDebugInfo: "",
    },
    async mounted() {
        start = performance.now();
        try {
            await this.getAllStaticData();
            this.getFieldsFromPageParameters();
            await this.runQuery();
        }
        finally {
            this.mountFinished = true;
            this.mountDebugInfo = "Vue mount time: " + ((performance.now() - start) / 1000).toFixed(1) + " seconds";
        }
    },
    methods: {
        async getAllStaticData() {
            await axios.get('/Search/GetAllStaticData/')
                .then(result => {
                    this.allStaticData = result.data;
                    this.possibleDecks = result.data.userDecks;
                    this.possibleHeapsForMove = result.data.possibleHeapsForMove;
                    this.singleDeckDisplayOptimization = result.data.userDecks.length == 2; //2 because there is a fake deck in the list, for ignore
                    this.selectedDeck = this.possibleDecks[0];
                    this.possibleDecksInclusionChoices = [{ selectedDeckIsInclusive: true, choiceText: result.data.localizedText.includeCards }, { selectedDeckIsInclusive: false, choiceText: result.data.localizedText.excludeCards }];
                    this.addToDeckDatePossibleChoices = [{ choiceId: 0, choiceText: result.data.localizedText.ignore }, { choiceId: 1, choiceText: result.data.localizedText.before }, { choiceId: 2, choiceText: result.data.localizedText.thatDay }, { choiceId: 3, choiceText: result.data.localizedText.after }];
                    this.expiryDatePossibleChoices = [{ choiceId: 0, choiceText: result.data.localizedText.ignore }, { choiceId: 1, choiceText: result.data.localizedText.before }, { choiceId: 2, choiceText: result.data.localizedText.thatDay }, { choiceId: 3, choiceText: result.data.localizedText.after }];
                    this.possibleOwners = result.data.allUsers;
                    this.selectedOwner = this.guidNoUserFiltering;
                    this.visibilityFilteringPossibleChoices = [{ choiceId: 1, choiceText: this.allStaticData.localizedText.ignore }, { choiceId: 2, choiceText: this.allStaticData.localizedText.cardVisibleToMoreThanOwner }, { choiceId: 3, choiceText: this.allStaticData.localizedText.cardVisibleToOwnerOnly }];
                    this.possibleTargetDecksForAdd = result.data.allDecksForAddingCards;
                    this.possibleRatingFilteringKind = [{ choiceId: 1, choiceText: this.allStaticData.localizedText.ignore }, { choiceId: 2, choiceText: this.allStaticData.localizedText.selectedRatingAndAbove }, { choiceId: 3, choiceText: this.allStaticData.localizedText.selectedRatingAndBelow }, { choiceId: 4, choiceText: this.allStaticData.localizedText.noRating }];
                    this.possibleRatingFilteringValues = [{ choiceId: 1, choiceText: ratingAsStars(1) }, { choiceId: 2, choiceText: ratingAsStars(2) }, { choiceId: 3, choiceText: ratingAsStars(3) }, { choiceId: 4, choiceText: ratingAsStars(4) }, { choiceId: 5, choiceText: ratingAsStars(5) },];
                    this.possibleSelectedNotificationFiltering = [{ choiceId: 1, choiceText: this.allStaticData.localizedText.ignore }, { choiceId: 2, choiceText: this.allStaticData.localizedText.cardsRegisteredForNotif }, { choiceId: 3, choiceText: this.allStaticData.localizedText.cardsNotRegisteredForNotif }];
                    this.updateFieldsAccordingToDeck();
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        getFieldsFromPageParameters() {
            deckParam = document.getElementById("DeckIdInput").value;
            for (let i = 0; i < this.possibleDecks.length; i++)
                if (this.possibleDecks[i].deckId == deckParam)
                    this.selectedDeck = this.possibleDecks[i];
            this.updateFieldsAccordingToDeck();

            wantedHeap = document.getElementById("HeapIdInput").value;
            if (wantedHeap) {
                this.selectedHeap = parseInt(wantedHeap);
                this.resetSelectedHeapIfNotValid();
            }

            wantedTag = document.getElementById("TagFilterInput").value;
            if (wantedTag) {
                for (let i = 0; i < this.possibleRequiredTags.length; i++)
                    if (this.possibleRequiredTags[i].tagId == wantedTag)
                        this.selectedRequiredTags.push(this.possibleRequiredTags[i]);
            }
        },
        getRequest() {
            return {
                pageNo: this.pageNo,
                pageSize: this.pageSize,
                requiredTags: this.selectedRequiredTags.map(tag => tag.tagId),
                excludedTags: this.selectedExcludedTags.map(tag => tag.tagId),
                requiredText: this.textFilter,
                deck: this.selectedDeck ? this.selectedDeck.deckId : this.guidNoDeckFiltering,
                deckIsInclusive: this.deckSelectionIsInclusive,
                heap: this.selectedHeap,
                visibility: this.selectedVisibilityFilteringChoice,
                ratingFilteringMode: this.selectedAverageRatingFilteringKind,
                ratingFilteringValue: this.selectedAverageRatingFilteringValue,
                notificationFiltering: this.selectedNotificationFilteringId,
            };
        },
        async runQuery() {
            if (this.runResult.cardsWithSelectionInfo && this.runResult.cardsWithSelectionInfo.length > 0)
                this.runResult.cardsWithSelectionInfo = [];

            this.loadingQuery = true;
            this.runResult.pageCount = 0;
            this.runResult.cardsWithSelectionInfo = [];

            request = this.getRequest();

            start = performance.now();
            await axios.post('/Search/RunQuery/', request)
                .then(result => {
                    this.lastQueryDebugInfo = "Query run time: " + ((performance.now() - start) / 1000).toFixed(1) + " seconds";
                    this.runResult.pageCount = result.data.pageCount;
                    this.runResult.totalNbCard = result.data.totalNbCards;

                    for (let i = 0; i < result.data.cards.length; i++) {
                        this.runResult.cardsWithSelectionInfo.push({
                            selected: false,
                            card: this.cardFromControllerResult(result.data.cards[i])
                        });
                    };
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });

            this.loadingQuery = false;

            if (this.runResult.pageCount != 0 && this.pageNo > this.runResult.pageCount) {
                this.pageNo = 1;
                if (this.runResult.totalNbCard > 0)
                    await this.runQuery();
            }
        },
        filteringOnDeckInclusive() {
            result = this.selectedDeck && (this.selectedDeck.deckId != this.possibleDecks[0].deckId) && this.deckSelectionIsInclusive;
            return result;
        },
        cardFromControllerResult(controllerResultCard) {    //controllerResultCard a SearchController.RunQueryCardViewModel
            result = controllerResultCard;
            result.ratingShort = "\u2606" + controllerResultCard.averageRating;
            result.userRatingAsStars = ratingAsStars(controllerResultCard.currentUserRating);
            const truncatedAverage = Math.trunc(controllerResultCard.averageRating);
            result.averageRatingAsStars = ratingAsStars(truncatedAverage);
            return result;
        },
        possibleDecksInclusionChoicesEnabled() {
            return this.selectedDeck && (this.selectedDeck != this.possibleDecks[0]);
        },
        canMovePage(shift) {
            return (this.pageNo + shift > 0) && (this.pageNo + shift <= this.runResult.pageCount);
        },
        async moveToFirstPage() {
            this.pageNo = 1;
            await this.runQuery();
        },
        async moveToLastPage() {
            this.pageNo = this.runResult.pageCount;
            await this.runQuery();
        },
        async movePage(shift) {
            this.pageNo = this.pageNo + shift;
            await this.runQuery();
        },
        requestContainsRequiredTag(tag) {
            return this.selectedRequiredTags.some(t => t == tag);
        },
        requestContainsRequiredTagWithId(tagId) {
            return this.selectedRequiredTags.some(t => t.tagId == tagId);
        },
        CanAddSelectedRequiredTag() {
            result = this.selectedRequiredTagToAdd && !this.requestContainsRequiredTag(this.selectedRequiredTagToAdd);
            return result;
        },
        addRequiredTag() {
            if (this.CanAddSelectedRequiredTag()) {
                if (this.selectedRequiredTagToAdd.tagId == this.guidNoTagFiltering) {
                    this.selectedRequiredTags = [];
                    return;
                }
                this.selectedRequiredTags.push(this.selectedRequiredTagToAdd);
                sortTagArray(this.selectedRequiredTags);
            }
        },
        removeRequiredTag(index) {
            this.selectedRequiredTags.splice(index, 1);
        },
        requestContainsExcludedTag(tag) {
            return this.selectedExcludedTags.some(t => t == tag);
        },
        requestContainsExcludedTagWithId(tagId) {
            return this.selectedExcludedTags.some(t => t.tagId == tagId);
        },
        CanAddSelectedExcludedTag() {
            result = this.selectedExcludedTagToAdd && !this.requestContainsExcludedTag(this.selectedExcludedTagToAdd);
            return result;
        },
        addExcludedTag() {
            if (this.CanAddSelectedExcludedTag()) {
                if (this.selectedExcludedTagToAdd.tagId == this.guidExcludeAllTagFiltering) {
                    this.selectedExcludedTags = [this.selectedExcludedTagToAdd];
                    return;
                }
                if (this.selectedExcludedTagToAdd.tagId == this.guidNoTagFiltering) {
                    this.selectedExcludedTags = [];
                    return;
                }
                if (this.requestContainsExcludedTagWithId(this.guidExcludeAllTagFiltering)) {
                    this.selectedExcludedTags = [];
                }
                this.selectedExcludedTags.push(this.selectedExcludedTagToAdd);
                sortTagArray(this.selectedExcludedTags);
            }
        },
        removeExcludedTag(index) {
            this.selectedExcludedTags.splice(index, 1);
        },
        resetSelectedHeapIfNotValid() {
            for (let i = 0; i < this.possibleHeaps.length; i++)
                if (this.possibleHeaps[i].heapId === this.selectedHeap)
                    return;
            this.selectedHeap = -1; //Should not happen, this is a security, eg if the html parameter is not an int
        },
        updateFieldsAccordingToDeck() {
            if (this.selectedDeck.deckId == this.guidNoDeckFiltering || !this.deckSelectionIsInclusive) {
                this.possibleHeaps = [];
                this.possibleRequiredTags = this.allStaticData.allRequirableTags;
                this.possibleExcludedTags = this.allStaticData.allExcludableTags;
            }
            else {
                this.possibleRequiredTags = this.selectedDeck.requirableTags;
                this.possibleExcludedTags = this.selectedDeck.excludableTags;
                this.possibleHeaps = this.selectedDeck.heaps;
            }

            this.resetSelectedHeapIfNotValid();
            this.selectedAddToDeckDateChoice = 0;
            this.selectedExpiryDateChoice = 0;
            this.selectedRequiredTags = [];
            this.selectedRequiredTagToAdd = "";
            this.selectedExcludedTags = [];
            this.selectedExcludedTagToAdd = "";
        },
        htmlClassForCard(c) {   //c is SearchController.RunQueryCardViewModel
            //We need a class name for an HMTL which uniquely identifies a card, in order to be able to collapse it individually
            return 'c' + c.cardId.toString().replace(/-/g, '');
        },
        atLeastOneCardSelected() {
            if (!this.runResult.cardsWithSelectionInfo)
                return false;
            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++)
                if (this.runResult.cardsWithSelectionInfo[i].selected)
                    return true;
            return false;
        },
        selectionCount() {
            if (!this.runResult.cardsWithSelectionInfo)
                return 0;
            var result = 0;
            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++)
                if (this.runResult.cardsWithSelectionInfo[i].selected)
                    result++;
            return result;
        },
        addTagDropDownButtonEnabled() {
            if (!this.atLeastOneCardSelected()) {
                this.addTagDropDownButtonDisabledReason = this.allStaticData.localizedText.operationIsForSelectedCards;
                return false;
            }
            return true;
        },
        deckActionDropDownButtonEnabled() {
            result = this.atLeastOneCardSelected();
            return result;
        },
        getSelectedCardIds() {
            result = [];
            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++)
                if (this.runResult.cardsWithSelectionInfo[i].selected)
                    result.push(this.runResult.cardsWithSelectionInfo[i].card.cardId);
            return result;
        },
        async addTagToSelectedCards(tag) {
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }
            mesg = this.allStaticData.localizedText.alertAddTagToCardsPart1 + ' ' + tag.tagName + ' ' + this.allStaticData.localizedText.alertAddTagToCardsPart2 + ' ' + selectedCardIds.length + ' ';
            if (selectedCardIds.length == 1)
                mesg += this.allStaticData.localizedText.alertAddTagToCardsPart3Single;
            else
                mesg += this.allStaticData.localizedText.alertAddTagToCardsPart3Plural;
            if (confirm(mesg)) {
                this.loadingQuery = true;

                await axios.post('/Search/AddTagToCards/' + tag.tagId, { cardIds: selectedCardIds })
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });

                this.loadingQuery = false;
                this.runQuery();
            }
        },
        selectAll() {
            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++)
                this.runResult.cardsWithSelectionInfo[i].selected = true;
        },
        unselectAll() {
            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++)
                this.runResult.cardsWithSelectionInfo[i].selected = false;
        },
        onSelectAllCheckBoxClick() {
            check = document.getElementById("selectAllCheckBox").checked;
            if (check)
                this.selectAll();
            else
                this.unselectAll();
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        async addSelectedCardsToDeck(deck) {     //deck is SearchController.GetAllStaticDataDeckForAddViewModel
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }

            nbCardsAlreadyInDeck = 0;
            for (let cardsIndex = 0; cardsIndex < this.runResult.cardsWithSelectionInfo.length; cardsIndex++)
                if (this.runResult.cardsWithSelectionInfo[cardsIndex].selected && this.cardIsInDeck(this.runResult.cardsWithSelectionInfo[cardsIndex].card, deck.deckId))
                    nbCardsAlreadyInDeck++;

            if (selectedCardIds.length == nbCardsAlreadyInDeck) {
                if (selectedCardIds.length == 1)
                    mesg = this.allStaticData.localizedText.cardAlreadyInDeck;
                else
                    mesg = this.allStaticData.localizedText.cardsAlreadyInDeck;
                alert(mesg + " " + deck.deckName);
                return;
            }

            if (selectedCardIds.length == 1)
                mesg = this.allStaticData.localizedText.alertAddOneCardToDeck + ' ' + deck.deckName;
            else {
                cardsToAddCount = selectedCardIds.length - nbCardsAlreadyInDeck;
                if (cardsToAddCount == 1)
                    mesg = this.allStaticData.localizedText.alertAddCardToDeckPart1;
                else
                    mesg = this.allStaticData.localizedText.alertAddCardsToDeckPart1 + ' ' + cardsToAddCount + ' ' + this.allStaticData.localizedText.alertAddCardsToDeckPart2;
                mesg = mesg + ' ' + deck.deckName;

                if (nbCardsAlreadyInDeck == 1)
                    mesg = mesg + " (" + this.allStaticData.localizedText.alertAddCardToDeckPart3 + ")";
                else
                    if (nbCardsAlreadyInDeck > 1)
                        mesg = mesg + " (" + nbCardsAlreadyInDeck + ' ' + this.allStaticData.localizedText.alertAddCardsToDeckPart3 + ")";
            }
            mesg = mesg + " ?";

            if (confirm(mesg)) {
                this.loadingQuery = true;

                await axios.post('/Search/AddCardsToDeck/' + deck.deckId, { cardIds: selectedCardIds })
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        alert(error.response.data.detail);
                    });

                this.loadingQuery = false;
                this.runQuery();
            }
        },
        cardIsInDeck(card, deckId) {    //card is SearchController.RunQueryCardViewModel
            var cardDecks = card.decks;
            for (let decksIndex = 0; decksIndex < cardDecks.length; decksIndex++)
                if (cardDecks[decksIndex].deckId == deckId)
                    return true;
            return false;
        },
        async removeSelectedCardsFromDeck(deck) {     //deck is SearchController.GetAllStaticDataDeckForAddViewModel
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }

            nbCardsNotInDeck = 0;
            for (let cardsIndex = 0; cardsIndex < this.runResult.cardsWithSelectionInfo.length; cardsIndex++)
                if (this.runResult.cardsWithSelectionInfo[cardsIndex].selected && !this.cardIsInDeck(this.runResult.cardsWithSelectionInfo[cardsIndex].card, deck.deckId))
                    nbCardsNotInDeck++;

            if (selectedCardIds.length == nbCardsNotInDeck) {
                if (selectedCardIds.length == 1)
                    mesg = this.allStaticData.localizedText.cardAlreadyNotInDeck;
                else
                    mesg = this.allStaticData.localizedText.cardsAlreadyNotInDeck;
                alert(mesg + " " + deck.deckName);
                return;
            }

            if (selectedCardIds.length == 1)
                mesg = this.allStaticData.localizedText.alertRemoveOneCardFromDeck + ' ' + deck.deckName;
            else {
                cardsToRemoveCount = selectedCardIds.length - nbCardsNotInDeck;
                if (cardsToRemoveCount == 1)
                    mesg = this.allStaticData.localizedText.alertRemoveCardFromDeckPart1;
                else
                    mesg = this.allStaticData.localizedText.alertRemoveCardsFromDeckPart1 + ' ' + cardsToRemoveCount + ' ' + this.allStaticData.localizedText.alertRemoveCardsFromDeckPart2;
                mesg = mesg + ' ' + deck.deckName;

                if (nbCardsNotInDeck == 1)
                    mesg = mesg + " (" + this.allStaticData.localizedText.alertRemoveCardFromDeckPart3 + ")";
                else
                    if (nbCardsNotInDeck > 1)
                        mesg = mesg + " (" + nbCardsNotInDeck + ' ' + this.allStaticData.localizedText.alertRemoveCardsFromDeckPart3 + ")";
            }
            mesg = mesg + " ?";

            if (confirm(mesg)) {
                this.loadingQuery = true;

                await axios.post('/Search/RemoveCardsFromDeck/' + deck.deckId, { cardIds: selectedCardIds })
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });

                this.loadingQuery = false;
                this.runQuery();
            }
        },
        addToDeckEnabled() {
            return !this.filteringOnDeckInclusive() && (this.possibleTargetDecksForAdd.length > 0);
        },
        moveToHeapEnabled() {
            return this.filteringOnDeckInclusive();
        },
        async moveSelectedCardsToHeap(targetHeap) {
            if (!this.filteringOnDeckInclusive()) {
                alert(this.allStaticData.localizedText.operationIsForFilteringOnDeckInclusive);
                return;
            }

            selectedCardIds = [];
            nbSelectedCardsAlreadyInTargetHeap = 0;

            for (let i = 0; i < this.runResult.cardsWithSelectionInfo.length; i++) {
                const cardWithSelectionInfo = this.runResult.cardsWithSelectionInfo[i];
                if (cardWithSelectionInfo.selected) {
                    selectedCardIds.push(cardWithSelectionInfo.card.cardId);
                    if (cardWithSelectionInfo.card.decks[0].heapId == targetHeap.heapId) //We know we are filtering on deck inclusive, so the card is exactly in one deck
                        nbSelectedCardsAlreadyInTargetHeap++;
                }
            }

            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }

            if (selectedCardIds.length == nbSelectedCardsAlreadyInTargetHeap) {
                if (selectedCardIds.length == 1)
                    mesg = this.allStaticData.localizedText.cardAlreadyInTargetHeap;
                else
                    mesg = this.allStaticData.localizedText.cardsAlreadyInTargetHeap;
                alert(mesg + " " + targetHeap.heapName);
                return;
            }

            if (selectedCardIds.length == 1)
                mesg = this.allStaticData.localizedText.alertMoveOneCardToHeap + ' ' + targetHeap.heapName;
            else {
                cardsToMoveCount = selectedCardIds.length - nbSelectedCardsAlreadyInTargetHeap;
                if (cardsToMoveCount == 1)
                    mesg = this.allStaticData.localizedText.alertMoveOneCardToHeap;
                else
                    mesg = this.allStaticData.localizedText.alertMoveCardsToHeapPart1 + ' ' + cardsToMoveCount + ' ' + this.allStaticData.localizedText.alertMoveCardsToHeapPart2;
                mesg = mesg + ' ' + targetHeap.heapName;

                if (nbSelectedCardsAlreadyInTargetHeap == 1)
                    mesg = mesg + " (" + this.allStaticData.localizedText.alertMoveCardToHeapPart3 + ")";
                else
                    if (nbSelectedCardsAlreadyInTargetHeap > 1)
                        mesg = mesg + " (" + nbSelectedCardsAlreadyInTargetHeap + ' ' + this.allStaticData.localizedText.alertMoveCardsToHeapPart3 + ")";
            }
            mesg = mesg + " ?";

            if (confirm(mesg)) {
                this.loadingQuery = true;

                await axios.post('/Search/MoveCardsToHeap/' + this.selectedDeck.deckId + '/' + targetHeap.heapId, { cardIds: selectedCardIds })
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });

                this.loadingQuery = false;
                this.runQuery();
            }
        },
        showDebugInfo() {
            return this.allStaticData.showDebugInfo;
        },
        filteringOnOwnerCurrentUser() {
            return (this.allStaticData.currentUserId != this.guidNoCurrentUser); //to be implemented: && (this.selectedOwner == this.allStaticData.currentUserId);
        },
        nonDeckActionDropDownButtonEnabled() {
            return this.atLeastOneCardSelected();
        },
        deleteCardEnabled() {
            //While not absolutely necessary, checking that we are filtering on current user owner of the cards is a second security against misunderstanding by the user of what delete means
            //(I say not absolutely necessary because the server side app is going to do all the checks before deleting the cards)
            return this.filteringOnOwnerCurrentUser();
        },
        async deletedSelectedCards() {
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }
            mesg = this.allStaticData.localizedText.alertDeleteCardsPart1;
            if (selectedCardIds.length == 1)
                mesg += " " + this.allStaticData.localizedText.alertDeleteCardsPart2Single;
            else
                mesg += selectedCardIds.length + ' ' + this.allStaticData.localizedText.alertDeleteCardsPart2Plural;
            if (confirm(mesg)) {
                this.loadingQuery = true;

                await axios.post('/Search/DeleteCards', { cardIds: selectedCardIds })
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });

                this.loadingQuery = false;
                this.runQuery();
            }
        },
        averageRatingFilteringEnabled() {
            return this.selectedAverageRatingFilteringKind != 1 && this.selectedAverageRatingFilteringKind != 4;
        },
        registerForNotificationsEnabled() {
            return this.selectedNotificationFilteringId != 2;
        },
        async registerForNotifications() {
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }
            this.loadingQuery = true;
            await axios.post('/Search/RegisterForNotifications', { cardIds: selectedCardIds })
                .then(result => {
                    tellControllerSuccess(result, this);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
            this.loadingQuery = false;
            if (this.selectedNotificationFilteringId != 1)
                //For example, the user is filtering on cards not registered. If he registers some cards, the query needs to be run again
                this.runQuery();
        },
        unRegisterForNotificationsEnabled() {
            return this.selectedNotificationFilteringId != 3;
        },
        async unRegisterForNotifications() {
            selectedCardIds = this.getSelectedCardIds();
            if (selectedCardIds.length == 0) {
                alert(this.allStaticData.localizedText.operationIsForSelectedCards);
                return;
            }
            this.loadingQuery = true;
            await axios.post('/Search/UnregisterForNotifications', { cardIds: selectedCardIds })
                .then(result => {
                    tellControllerSuccess(result, this);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
            this.loadingQuery = false;
            if (this.selectedNotificationFilteringId != 1)
                //For example, the user is filtering on cards registered. If he unregisters some cards, the query needs to be run again
                this.runQuery();
        },
        async subscribe() {
            if (confirm(this.allStaticData.localizedText.confirmSubscription)) {
                request = this.getRequest();

                await axios.post('/Search/SubscribeToSearch/', request)
                    .then(result => {
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });
            }
        },
    },
    watch: {
        selectedDeck: {
            handler() {
                this.updateFieldsAccordingToDeck();
            },
        },
        deckSelectionIsInclusive: {
            handler() {
                this.updateFieldsAccordingToDeck();
            },
        },
        selectedRequiredTagToAdd: {
            handler() {
                this.addRequiredTag();
                this.selectedRequiredTagToAdd = "";
            }
        },
        selectedExcludedTagToAdd: {
            handler() {
                this.addExcludedTag();
                this.selectedExcludedTagToAdd = "";
            }
        },
    },
});
