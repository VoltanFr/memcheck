var app = new Vue({
    el: '#LearnMainDiv',
    data: {
        userDecks: [],  //LearnController.UserDecksViewModel
        activeDeck: null,  //LearnController.UserDecksViewModel
        singleDeckDisplay: false,
        learningUnknowns: true, //If false we are in repeat expired mode
        currentCard: null,    //LearnController.GetCardsCardViewModel
        backSideVisible: false,
        mountFinished: false,
        loading: false,
        currentFullScreenImage: null,   //LearnController.GetCardsImageViewModel
        pendingMoveOperations: [],  //{deckId: Guid, cardId: Guid, targetHeap: int, manualMove: bool, nbAttempts: int}
        currentMovingCard: null,    //{deckId: Guid, cardId: Guid, targetHeap: int, manualMove: bool, nbAttempts: int}
        currentMovePromise: null, //promise
        pendingRatingOperations: [],  //{cardId: Guid, rating: int, nbAttempts: int}
        currentRatingPromise: null, //promise
        pendingNotificationRegistrations: [],  //{cardId: Guid, notify: bool}
        currentNotificationRegistrationPromise: null, //promise
        downloadedCards: [],    //LearnController.GetCardsCardViewModel
        cardDownloadOperation: null,
        currentImageLoadingPromise: null,
        filteringDisplay: false,
        selectedExcludedTags: [],   //LearnController.GetAllStaticDataTagViewModel
        selectedExcludedTagToAdd: "",   //LearnController.GetAllStaticDataTagViewModel. Model of the combo box, used to manage adding
        guidNoTagFiltering: '00000000-0000-0000-0000-000000000000',
        userQuitAttemptDisplay: false,
        lastDownloadIsEmpty: false,
        bigSizeImageLabels: null,   //MediaController.GetBigSizeImageLabels
        additionalMoveDebugInfo: null,
        additionalRatingDebugInfo: null,
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            getBigSizeImageLabelsTask = this.GetBigSizeImageLabels();
            await this.GetUserDecks();
            this.GetLearnModeFromPageParameter();
            this.downloadCardsIfNeeded();
            if (this.cardDownloadOperation)
                await this.cardDownloadOperation;
            this.getCard();
            await getBigSizeImageLabelsTask;
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener("popstate", this.onPopState);
        document.removeEventListener("beforeunload", this.onBeforeUnload);
    },
    methods: {
        GetLearnModeFromPageParameter() {
            //There has to be a better way, but here's how I get a parameter passed to a page
            wantedLearnMode = document.getElementById("LearnModeInput").value;
            this.learningUnknowns = wantedLearnMode == "Unknown";
        },
        async GetUserDecks() {
            await axios.get('/Learn/UserDecks/')
                .then(result => {
                    this.userDecks = result.data;
                    if (this.userDecks.length === 1) {
                        this.activeDeck = this.userDecks[0];
                        this.singleDeckDisplay = true;
                    }
                    else {
                        this.activeDeck = "";
                        this.singleDeckDisplay = false;
                    }
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        getCard() {
            this.backSideVisible = false;
            this.currentCard = null;
            if (!this.activeDeck)
                return;

            for (let i = 0; !this.currentCard && i < this.downloadedCards.length; i++)
                if (this.cardIsReady(this.downloadedCards[i])) {
                    var spliced = this.downloadedCards.splice(i, 1);
                    this.currentCard = spliced[0];
                }
        },
        cardIsReady(card) { //card is an entry of downloadedCards
            //A card is ready when all its images have been loaded (or it has no image)
            for (let i = 0; i < card.images.length; i++)
                if (!card.images[i].blob)
                    return false;
            return true;
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        openSettingsPage() {
            if (this.activeDeck)
                window.location.href = '/Decks/Settings?DeckId=' + this.activeDeck.deckId;
        },
        showBackSide() {
            this.backSideVisible = true;
        },
        knew() {
            this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: this.currentCard.heapId + 1, manualMove: false, nbAttempts: 0 });
            this.getCard();
        },
        forgot() {
            this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: 0, manualMove: false, nbAttempts: 0 });
            this.getCard();
        },
        editCard() {
            window.location.href = '/Authoring?CardId=' + this.currentCard.cardId + "&ReturnUrl=" + window.location;
        },
        spawnDownloadImage(image) {//image is LearnController.GetCardsImageViewModel
            this.currentImageLoadingPromise = axios.get('/Learn/GetImage/' + image.imageId + "/2", { responseType: 'arraybuffer' })
                .then(result => {
                    image.blob = base64FromBytes(result.data);
                    this.currentImageLoadingPromise = null;
                    if (!this.currentCard)
                        this.getCard();
                })
                .catch(error => {
                    this.currentImageLoadingPromise = null;
                });
        },
        async removeCard() {
            if (confirm(this.currentCard.removeAlertMessage + this.dt(this.currentCard.addToDeckUtcTime))) {
                await axios.delete('/Decks/RemoveCardFromDeck/' + this.activeDeck.deckId + "/" + this.currentCard.cardId)
                    .then(result => {
                        this.getCard();
                        tellControllerSuccess(result, this);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    })
            }
        },
        showImageFull(image) {  //image is LearnController.GetCardsImageViewModel
            this.currentFullScreenImage = image;
        },
        handlePendingMoveOperations() {
            if (!this.currentMovePromise && this.pendingMoveOperations.length > 0) {
                this.currentMovingCard = this.pendingMoveOperations.shift();
                this.additionalMoveDebugInfo = "Moving (cardid: " + this.currentMovingCard.cardId + ", target heap: " + this.currentMovingCard.targetHeap + ", nbAttempts: " + this.currentMovingCard.nbAttempts + ")";
                const url = '/Learn/MoveCardToHeap/' + this.currentMovingCard.deckId + '/' + this.currentMovingCard.cardId + '/' + this.currentMovingCard.targetHeap + '/' + this.currentMovingCard.manualMove;
                const timeOut = Math.min(60000, (this.currentMovingCard.nbAttempts + 1) * 1000);

                this.currentMovePromise = pachAxios(url, timeOut)
                    .then(result => {
                        this.currentMovePromise = null;
                        this.additionalMoveDebugInfo = "Moved (cardid: " + this.currentMovingCard.cardId + ", target heap: " + this.currentMovingCard.targetHeap + ", nbAttempts: " + this.currentMovingCard.nbAttempts + ")";
                        this.currentMovingCard = null;
                        if (this.timeToExitPage())
                            window.location.href = '/';
                    })
                    .catch(error => {
                        this.additionalMoveDebugInfo = "Move failed, will retry in 1 sec (cardid: " + this.currentMovingCard.cardId + ", target heap: " + this.currentMovingCard.targetHeap + ", nbAttempts: " + this.currentMovingCard.nbAttempts + ")";

                        sleep(1000).then(() => {
                            this.additionalMoveDebugInfo = "Move failed, will retry asap (cardid: " + this.currentMovingCard.cardId + ", target heap: " + this.currentMovingCard.targetHeap + ", nbAttempts: " + this.currentMovingCard.nbAttempts + ")";
                            this.currentMovePromise = null;
                            this.pendingMoveOperations.push({ deckId: this.currentMovingCard.deckId, cardId: this.currentMovingCard.cardId, targetHeap: this.currentMovingCard.targetHeap, manualMove: this.currentMovingCard.manualMove, nbAttempts: this.currentMovingCard.nbAttempts + 1 });
                            this.currentMovingCard = null;
                        })
                    });
            }
        },
        downloadCardsIfNeeded() {
            if (this.activeDeck && !this.cardDownloadOperation && this.downloadedCards.length < 30) {
                var excludedCardIds = this.downloadedCards.map(card => { return card.cardId; });
                if (this.currentMovePromise)
                    excludedCardIds.push(this.currentMovingCard.cardId);
                if (this.currentCard)
                    excludedCardIds.push(this.currentCard.cardId);
                for (let i = 0; i < this.pendingMoveOperations.length; i++)
                    excludedCardIds.push(this.pendingMoveOperations[i].cardId);

                const query = {
                    deckId: this.activeDeck.deckId,
                    learnModeIsUnknown: this.learningUnknowns,
                    excludedCardIds: excludedCardIds,
                    excludedTagIds: this.selectedExcludedTags.map(tag => tag.tagId),
                    currentCardCount: this.downloadedCards.length
                };

                this.cardDownloadOperation = axios.post('/Learn/GetCards', query)
                    .then(result => {
                        if (result.data.cards.length == 0) {
                            if (this.timeToExitPage())
                                window.location.href = '/';
                            this.lastDownloadIsEmpty = true;
                        }
                        for (let i = 0; i < result.data.cards.length; i++)
                            this.downloadedCards.push(result.data.cards[i]);
                        this.cardDownloadOperation = null;
                    })
                    .catch(error => {
                        const sleep = (milliseconds) => {
                            return new Promise(resolve => setTimeout(resolve, milliseconds))
                        }

                        sleep(1000).then(() => {
                            this.cardDownloadOperation = null;
                        })
                    });
            }
        },
        downloadImagesIfNeeded() {
            if (this.activeDeck) {
                for (let cardIndex = 0; !this.currentImageLoadingPromise && cardIndex < this.downloadedCards.length; cardIndex++) {
                    var card = this.downloadedCards[cardIndex];
                    for (let imageIndex = 0; !this.currentImageLoadingPromise && imageIndex < card.images.length; imageIndex++)
                        if (!card.images[imageIndex].blob) {
                            this.spawnDownloadImage(card.images[imageIndex]);
                        }
                }
            }
        },
        preventQuittingPage() {
            return !this.canExitPageSafely();
        },
        switchToFilteringMode() {
            this.filteringDisplay = true;
        },
        async closeFilteringMode() {
            this.filteringDisplay = false;
            this.loading = true;
            this.currentCard = null;
            this.backSideVisible = false;
            this.downloadedCards = [];
            this.cardDownloadOperation = null;
            this.currentImageLoadingPromise = null;
            this.downloadCardsIfNeeded();
            if (this.cardDownloadOperation)
                await this.cardDownloadOperation;
            this.getCard();
            this.loading = false;
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
                if (this.selectedExcludedTagToAdd.tagId == this.guidNoTagFiltering) {
                    this.selectedExcludedTags = [];
                    return;
                }
                this.selectedExcludedTags.push(this.selectedExcludedTagToAdd);
            }
        },
        removeExcludedTag(index) {
            this.selectedExcludedTags.splice(index, 1);
        },
        onBeforeUnload(event) {
            if (!this.canExitPageSafely()) {
                this.userQuitAttemptDisplay = true;
                (event || window.event).returnValue = "Some saving operations are not finished";
                return "Some saving operations are not finished";   //Message will not display on modern browers, but a fixed message will be displayed
            }
        },
        onPopState() {
            //If we are in full screen image mode, a state "#" has been pushed by the browser
            if (!document.location.href.endsWith('#'))
                this.currentFullScreenImage = null;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        showDebugInfo() {
            return this.userDecks.length > 0 && this.userDecks[0].showDebugInfo;
        },
        currentCardHasAdditionalSide() {
            return this.currentCard.additionalInfo || this.currentCard.images.some(img => img.cardSide == 3);
        },
        visibilityPopoverTarget() {
            if (this.currentCard.visibleToCount == 0)
                return "visibilityPopover0";
            if (this.currentCard.visibleToCount == 1)
                return "visibilityPopover1";
            return "visibilityPopover2";
        },
        moveToHeap(targetHeap) {    //GetCardsHeapModel
            const alertMesg = targetHeap.moveToAlertMessage + (targetHeap.expiryUtcDate == "0001-01-01T00:00:00Z" ? "" : (this.dt(targetHeap.expiryUtcDate + '.')));
            if (confirm(alertMesg)) {
                this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: targetHeap.heapId, manualMove: true, nbAttempts: 0 });
                this.getCard();
            }
        },
        enqueueRatingUpload() {
            this.pendingRatingOperations.push({ cardId: this.currentCard.cardId, rating: this.currentCard.currentUserRating, nbAttempts: 0 });
        },
        timeToExitPage() {
            result = !this.currentCard;
            result = result && this.downloadedCards.length == 0;
            result = result && this.lastDownloadIsEmpty;
            result = result && this.canExitPageSafely();
            return result;
        },
        canExitPageSafely() {
            result = this.pendingMoveOperations.length == 0;
            result = result && !this.currentMovePromise;
            result = result && this.pendingRatingOperations.length == 0;
            result = result && !this.currentRatingPromise;
            result = result && this.pendingNotificationRegistrations.length == 0;
            result = result && !this.currentNotificationRegistrationPromise;
            return result;
        },
        handlePendingRatingOperations() {
            if (!this.currentRatingPromise && this.pendingRatingOperations.length > 0) {
                var ratingOperation = this.pendingRatingOperations.shift();
                this.additionalRatingDebugInfo = "Recording rating (cardid: " + ratingOperation.cardId + ", rating: " + ratingOperation.rating + ", nbAttempts: " + ratingOperation.nbAttempts + ")";
                const url = '/Learn/SetCardRating/' + ratingOperation.cardId + '/' + ratingOperation.rating;
                const timeOut = Math.min(60000, (ratingOperation.nbAttempts + 1) * 1000);

                this.currentRatingPromise = pachAxios(url, timeOut)
                    .then(result => {
                        this.currentRatingPromise = null;
                        this.additionalRatingDebugInfo = "Rating recorded (cardid: " + ratingOperation.cardId + ", rating: " + ratingOperation.rating + ", nbAttempts: " + ratingOperation.nbAttempts + ")";
                        if (this.timeToExitPage())
                            window.location.href = '/';
                    })
                    .catch(error => {
                        this.additionalRatingDebugInfo = "Rating failed, will retry in 1 sec (cardid: " + ratingOperation.cardId + ", rating: " + ratingOperation.rating + ", nbAttempts: " + ratingOperation.nbAttempts + ")";
                        sleep(1000).then(() => {
                            this.additionalRatingDebugInfo = "Rating failed, will retry asap (cardid: " + ratingOperation.cardId + ", rating: " + ratingOperation.rating + ", nbAttempts: " + ratingOperation.nbAttempts + ")";
                            this.currentRatingPromise = null;
                            this.pendingRatingOperations.push({ cardId: ratingOperation.cardId, rating: ratingOperation.rating, nbAttempts: ratingOperation.nbAttempts + 1 });
                        })
                    });
            }
        },
        currentCardFrontSide() {
            return convertMarkdown(this.currentCard.frontSide);
        },
        currentCardBackSide() {
            return convertMarkdown(this.currentCard.backSide);
        },
        currentCardAdditionalInfo() {
            return convertMarkdown(this.currentCard.additionalInfo);
        },
        currentUserRatingAsStars() {
            if (!this.currentCard)
                return "";
            return ratingAsStars(this.currentCard.currentUserRating);
        },
        averageRatingAsStars() {
            if (!this.currentCard)
                return "";
            const truncated = Math.trunc(this.currentCard.averageRating);
            return ratingAsStars(truncated);
        },
        unregisterForNotif() {
            this.currentCard.registeredForNotifications = false;
            this.enqueueNotificationRegistrationChange();
        },
        registerForNotif() {
            this.currentCard.registeredForNotifications = true;
            this.enqueueNotificationRegistrationChange();
        },
        enqueueNotificationRegistrationChange() {
            this.pendingNotificationRegistrations.push({ cardId: this.currentCard.cardId, notify: this.currentCard.registeredForNotifications });
        },
        handlePendingNotificationRegistrations() {
            if (!this.currentNotificationRegistrationPromise && this.pendingNotificationRegistrations.length > 0) {
                var operation = this.pendingNotificationRegistrations.shift();

                this.currentNotificationRegistrationPromise = axios.patch('/Learn/SetCardNotificationRegistration/' + operation.cardId + '/' + operation.notify)
                    .then(result => {
                        this.currentNotificationRegistrationPromise = null;
                        if (this.timeToExitPage())
                            window.location.href = '/';
                    })
                    .catch(error => {
                        const sleep = (milliseconds) => {
                            return new Promise(resolve => setTimeout(resolve, milliseconds))
                        }

                        sleep(1000).then(() => {
                            this.currentNotificationRegistrationPromise = null;
                            this.pendingNotificationRegistrations.push(operation);
                        })
                    });
            }
        },
        async GetBigSizeImageLabels() {
            await axios.get('/Media/GetBigSizeImageLabels')
                .then(result => {
                    this.bigSizeImageLabels = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
    watch: {
        pendingMoveOperations: {
            handler() {
                this.handlePendingMoveOperations();
            },
        },
        currentMovePromise: {
            handler() {
                this.handlePendingMoveOperations();
            },
        },
        downloadedCards: {
            handler() {
                this.downloadCardsIfNeeded();
                this.downloadImagesIfNeeded();
            },
        },
        currentCardDownloadOperation: {
            handler() {
                this.downloadCardsIfNeeded();
            },
        },
        currentImageLoadingPromise: {
            handler() {
                this.downloadImagesIfNeeded();
            },
        },
        selectedExcludedTagToAdd: {
            handler() {
                this.addExcludedTag();
                this.selectedExcludedTagToAdd = "";
            }
        },
        pendingRatingOperations: {
            handler() {
                this.handlePendingRatingOperations();
            },
        },
        currentRatingPromise: {
            handler() {
                this.handlePendingRatingOperations();
            },
        },
        pendingNotificationRegistrations: {
            handler() {
                this.handlePendingNotificationRegistrations();
            },
        },
        currentNotificationRegistrationPromise: {
            handler() {
                this.handlePendingNotificationRegistrations();
            },
        },
    },
});
