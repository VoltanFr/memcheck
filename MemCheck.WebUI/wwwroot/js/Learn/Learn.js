import { BigSizeImage } from '../big-size-image.js';
import { TagButton } from '../TagButton.js';
import { CardRating } from '../CardRating.js';
import { isValidDateTime } from '../Common.js';
import { dateTime } from '../Common.js';
import { dateTimeWithTime } from '../Common.js';
import { emptyGuid } from '../Common.js';
import { imageSizeMedium } from '../Common.js';
import { imageSideAdditional } from '../Common.js';
import { toast } from '../Common.js';
import { tellAxiosError } from '../Common.js';
import { tellControllerSuccess } from '../Common.js';
import { base64FromBytes } from '../Common.js';
import { sleep } from '../Common.js';
import { pachAxios } from '../Common.js';
import { convertMarkdown } from '../MarkdownConversion.js';
import { getMnesiosImageNamesFromSourceText } from '../MarkdownConversion.js';

const learnModeExpired = 1;
const learnModeUnknown = 2;
const learnModeDemo = 3;
const maxCountOfCardsForDemo = 20;  // We stop when we have this count or more cards. Due to the downloading in batches, we will in fact have more. No problem with that.

const learnApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
        'van-popover': globalThis.vant.Popover,
        'van-rate': globalThis.vant.Rate,
        'big-size-image': BigSizeImage,
        'card-rating': CardRating,
        'tag-button': TagButton,
    },
    data() {
        return {
            showInfoPopover: false,
            showVisibilityPopover: false,
            heapInfoPopover: false,
            showMoveToHeapMenuPopover: false,
            userDecks: [],  // LearnController.UserDecksDeckViewModel
            activeDeck: null,  // LearnController.UserDecksViewModel
            singleDeckDisplay: false,
            currentCard: null,    // LearnController.GetCardsCardViewModel
            backSideVisible: false,
            mountFinished: false,
            loading: false,
            currentFullScreenImage: null,   // LearnController.GetCardsImageViewModel
            pendingMoveOperations: [],  // {deckId: Guid, cardId: Guid, targetHeap: int, manualMove: bool, nbAttempts: int}
            currentMovingCard: null,    // {deckId: Guid, cardId: Guid, targetHeap: int, manualMove: bool, nbAttempts: int}
            currentMovePromise: null, // promise
            pendingRatingOperations: [],  // {cardId: Guid, rating: int, nbAttempts: int}
            currentRatingPromise: null, // promise
            pendingNotificationRegistrations: [],  // {cardId: Guid, notify: bool}
            currentNotificationRegistrationPromise: null, // promise
            downloadedCards: [],    // LearnController.GetCardsCardViewModel
            remainingCardsInLesson: 0,
            cardDownloadOperation: null,
            currentImageLoadingPromise: null,
            currentImageDetailsLoadingPromise: null,
            userQuitAttemptDisplay: false,
            lastDownloadIsEmpty: false,
            additionalDebugInfo: null,
            learnMode: 0,   // can be learnModeExpired, learnModeUnknown, learnModeDemo
            tagIdForDemo: emptyGuid,
            reachedMaxCountOfCardsForDemo: false,
        };
    },
    beforeCreate() {
        this.dateTime = dateTime;
        this.dateTimeWithTime = dateTimeWithTime;
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            this.getLearnModeFromPageParameter();
            await this.getUserDecks();
            this.downloadCardsIfNeeded();
            if (this.cardDownloadOperation)
                await this.cardDownloadOperation;
            this.getCard();
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener('popstate', this.onPopState);
        document.removeEventListener('beforeunload', this.onBeforeUnload);
    },
    methods: {
        getLearnModeFromPageParameter() {
            // There has to be a better way, but here's how I get a parameter passed to a page
            const wantedLearnMode = document.getElementById('LearnModeInput').value;
            if (wantedLearnMode === 'Expired') {
                this.learnMode = learnModeExpired;
                return;
            }
            if (wantedLearnMode === 'Unknown') {
                this.learnMode = learnModeUnknown;
                return;
            }
            if (wantedLearnMode === 'Demo') {
                this.tagIdForDemo = document.getElementById('TagIdInput').value;
                if (!this.tagIdForDemo) {
                    // Demo mode tag not received
                    window.location.href = '/Index';
                }
                this.learnMode = learnModeDemo;
                return;
            }
            window.location.href = '/Index';
        },
        rehearsing() {
            return this.learnMode === learnModeExpired;
        },
        learningUnknowns() {
            return this.learnMode === learnModeUnknown;
        },
        demoMode() {
            return this.learnMode === learnModeDemo;
        },
        async getUserDecks() {
            await axios.get('/Learn/UserDecks/')
                .then(result => {
                    if (!this.demoMode() && !result.data.userLoggedIn)
                        window.location.href = '/Identity/Account/Login';
                    this.userDecks = result.data.decks;
                    this.singleDeckDisplay = this.demoMode || this.userDecks.length === 1;
                    this.activeDeck = this.userDecks.length === 1 ? this.userDecks[0] : '';
                })
                .catch(error => {
                    tellAxiosError(error, localized.FailedToGetUserDecks);
                    this.userDecks = [];
                });
        },
        getCard() {
            this.backSideVisible = false;
            this.currentCard = null;

            for (let i = 0; !this.currentCard && i < this.downloadedCards.length; i++)
                if (this.cardIsReady(this.downloadedCards[i])) {
                    const spliced = this.downloadedCards.splice(i, 1);
                    this.currentCard = spliced[0];
                }
        },
        cardIsReady(card) { // card is an entry of downloadedCards
            // A card is ready when all its images have been loaded (or it has no image)
            for (let i = 0; i < card.images.length; i++)
                if (!card.images[i].blob || !card.images[i].description)
                    return false;
            return true;
        },
        openDeckSettingsPage() {
            if (this.activeDeck)
                window.location.href = `/Decks/Settings?DeckId=${this.activeDeck.deckId}`;
        },
        showBackSide() {
            this.backSideVisible = true;
        },
        knew() {
            this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: this.currentCard.heapId + 1, manualMove: false, nbAttempts: 0 });
            if (this.demoMode())
                toast(localized.OnKnewToastMessage, localized.OnKnewToastTitle, true, 10000);
            this.getCard();
        },
        forgot() {
            this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: 0, manualMove: false, nbAttempts: 0 });
            if (this.demoMode())
                toast(localized.OnDidNotKnowToastMessage, localized.OnDidNotKnowToastTitle, true, 10000);
            this.getCard();
        },
        editUrl() {
            return `/Authoring?CardId=${this.currentCard.cardId}&ReturnAddress=${window.location}`;
        },
        spawnDownloadImageBlob(image) {// image is LearnController.GetCardsImageViewModel
            if (image.imageId) {
                // Old image format
                this.currentImageLoadingPromise = axios.get(`/Learn/GetImage/${image.imageId}/${imageSizeMedium}`, { responseType: 'arraybuffer' })
                    .then(result => {
                        image.blob = base64FromBytes(result.data);
                        this.currentImageLoadingPromise = null;
                        if (!this.currentCard)
                            this.getCard();
                    })
                    .catch(() => {
                        this.currentImageLoadingPromise = null;
                    });
            }
            else {
                const request = { imageName: image.name, size: imageSizeMedium };
                this.currentImageLoadingPromise = axios.post('/Learn/GetImageByName/', request, { responseType: 'arraybuffer' })
                    .then(result => {
                        image.blob = base64FromBytes(result.data);
                        this.currentImageLoadingPromise = null;
                        if (!this.currentCard)
                            this.getCard();
                    })
                    .catch(() => {
                        this.currentImageLoadingPromise = null;
                    });
            }
        },
        spawnDownloadImageDetails(image) {// image is LearnController.GetCardsImageViewModel
            const request = { imageName: image.name };
            this.currentImageDetailsLoadingPromise = axios.post('/Media/GetImageMetadataFromName/', request)
                .then(result => {
                    image.description = result.data.description;
                    image.source = result.data.source;
                    image.initialUploadUtcDate = result.data.initialUploadUtcDate;
                    image.initialVersionCreator = result.data.initialVersionCreator;
                    image.currentVersionUtcDate = result.data.currentVersionUtcDate;
                    image.currentVersionDescription = result.data.currentVersionDescription;
                    image.cardCount = result.data.cardCount;
                    image.originalImageContentType = result.data.originalImageContentType;
                    image.originalImageSize = result.data.originalImageSize;
                    image.smallSize = result.data.smallSize;
                    image.mediumSize = result.data.mediumSize;
                    image.bigSize = result.data.bigSize;
                    this.currentImageDetailsLoadingPromise = null;
                    if (!this.currentCard)
                        this.getCard();
                })
                .catch(() => {
                    this.currentImageDetailsLoadingPromise = null;
                });
        },
        onCardRemoved() {
        },
        async removeCard() {
            if (confirm(this.currentCard.removeAlertMessage + dateTime(this.currentCard.addToDeckUtcTime))) {
                await axios.delete(`/Decks/RemoveCardFromDeck/${this.activeDeck.deckId}/${this.currentCard.cardId}`)
                    .then(result => {
                        this.getCard();
                        tellControllerSuccess(result);
                    })
                    .catch(error => {
                        tellAxiosError(error, localized.FailedToRemoveCardFromDeck);
                    });
            }
        },
        showImageFull(image) {
            if (image === null) {
                learnAppInstance.additionalDebugInfo = 'showImageFull - image is null';
            }
            else {
                learnAppInstance.additionalDebugInfo = `showImageFull - image=${image} - image.description=${image.description}`;
            }
            learnAppInstance.currentFullScreenImage = image;
            history.pushState('ShowingImageDetails', 'BackToCard');
        },
        handlePendingMoveOperations() {
            if (!this.currentMovePromise && this.pendingMoveOperations.length > 0) {
                this.currentMovingCard = this.pendingMoveOperations.shift();

                if (this.demoMode()) {
                    this.currentMovingCard = null;
                    if (this.timeToExitPage())
                        window.location.href = '/';
                    else
                        this.updateRemainingCardsInLesson();
                    return;
                }

                const url = `/Learn/MoveCardToHeap/${this.currentMovingCard.deckId}/${this.currentMovingCard.cardId}/${this.currentMovingCard.targetHeap}/${this.currentMovingCard.manualMove}`;
                const timeOut = Math.min(60000, (this.currentMovingCard.nbAttempts + 1) * 1000);

                this.currentMovePromise = pachAxios(url, timeOut)
                    .then(() => {
                        this.currentMovePromise = null;
                        this.currentMovingCard = null;
                        if (this.timeToExitPage())
                            window.location.href = '/';
                        else
                            this.updateRemainingCardsInLesson();
                    })
                    .catch(() => {
                        sleep(1000).then(() => {
                            this.currentMovePromise = null;
                            this.pendingMoveOperations.push({ deckId: this.currentMovingCard.deckId, cardId: this.currentMovingCard.cardId, targetHeap: this.currentMovingCard.targetHeap, manualMove: this.currentMovingCard.manualMove, nbAttempts: this.currentMovingCard.nbAttempts + 1 });
                            this.currentMovingCard = null;
                        });
                    });
            }
        },
        downloadCardsIfNeeded() {
            if (this.demoMode() && this.reachedMaxCountOfCardsForDemo) {
                this.lastDownloadIsEmpty = true;
                this.updateRemainingCardsInLesson();
                return;
            }

            if ((this.demoMode() || this.activeDeck) && !this.cardDownloadOperation && this.downloadedCards.length < 30) {
                let excludedCardIds = this.downloadedCards.map(card => card.cardId);
                if (this.currentMovePromise)
                    excludedCardIds.push(this.currentMovingCard.cardId);
                if (this.currentCard)
                    excludedCardIds.push(this.currentCard.cardId);
                for (let i = 0; i < this.pendingMoveOperations.length; i++)
                    excludedCardIds.push(this.pendingMoveOperations[i].cardId);

                const query = {
                    deckId: this.activeDeck ? this.activeDeck.deckId : this.tagIdForDemo,
                    learnMode: this.learnMode,
                    excludedCardIds: excludedCardIds,
                    currentCardCount: this.downloadedCards.length
                };

                this.cardDownloadOperation = axios.post('/Learn/GetCards', query)
                    .then(result => {
                        if (result.data.cards.length === 0) {
                            if (this.timeToExitPage())
                                window.location.href = '/';
                            this.lastDownloadIsEmpty = true;
                        }
                        for (let i = 0; i < result.data.cards.length; i++) {
                            const card = result.data.cards[i];

                            // We don't discover images in card text on server side because we want the same code to be used at card authoring time
                            card.images = card.images.concat([...getMnesiosImageNamesFromSourceText(card.frontSide)].map(imageName => { return { imageId: null, name: imageName, blob: null, cardSide: 0 }; }));
                            card.images = card.images.concat([...getMnesiosImageNamesFromSourceText(card.backSide)].map(imageName => { return { imageId: null, name: imageName, blob: null, cardSide: 0 }; }));
                            card.images = card.images.concat([...getMnesiosImageNamesFromSourceText(card.additionalInfo)].map(imageName => { return { imageId: null, name: imageName, blob: null, cardSide: 0 }; }));
                            card.images = card.images.concat([...getMnesiosImageNamesFromSourceText(card.references)].map(imageName => { return { imageId: null, name: imageName, blob: null, cardSide: 0 }; }));

                            this.downloadedCards.push(card);
                        }
                        this.cardDownloadOperation = null;
                        this.updateRemainingCardsInLesson();
                    })
                    .catch(() => {
                        sleep(1000).then(() => {
                            this.cardDownloadOperation = null;  // Thanks to the watcher on cardDownloadOperation, a new download will be spawned
                        });
                    });
            }
        },
        downloadImagesIfNeeded() {
            for (let cardIndex = 0; !this.currentImageLoadingPromise && cardIndex < this.downloadedCards.length; cardIndex++) {
                const card = this.downloadedCards[cardIndex];
                for (let imageIndex = 0; !this.currentImageLoadingPromise && imageIndex < card.images.length; imageIndex++) {
                    if (!card.images[imageIndex].blob)
                        this.spawnDownloadImageBlob(card.images[imageIndex]);
                }
            }
            for (let cardIndex = 0; !this.currentImageDetailsLoadingPromise && cardIndex < this.downloadedCards.length; cardIndex++) {
                const card = this.downloadedCards[cardIndex];
                for (let imageIndex = 0; !this.currentImageDetailsLoadingPromise && imageIndex < card.images.length; imageIndex++) {
                    if (!card.images[imageIndex].description)
                        this.spawnDownloadImageDetails(card.images[imageIndex]);
                }
            }
        },
        preventQuittingPage() {
            return !this.canExitPageSafely();
        },
        onBeforeUnload(event) {
            if (!this.canExitPageSafely()) {
                this.userQuitAttemptDisplay = true;
                (event || window.event).returnValue = 'Some saving operations are not finished';
                return 'Some saving operations are not finished';   // Message will not display on modern browers, but a fixed message will be displayed
            }
            return null;
        },
        onPopState() {
            this.currentFullScreenImage = null;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        showDebugInfo() {
            return this.userDecks.length > 0 && this.userDecks[0].showDebugInfo;
        },
        currentCardHasAdditionalSide() {
            return this.currentCard.additionalInfo || this.currentCard.images.some(img => img.cardSide === imageSideAdditional);
        },
        currentCardHasReferences() {
            return this.currentCard.references;
        },
        moveToHeap(targetHeap) {    // GetCardsHeapModel
            const alertMesg = `${targetHeap.moveToAlertMessage} ${dateTime(targetHeap.expiryUtcDate)}.`;
            if (confirm(alertMesg)) {
                this.pendingMoveOperations.push({ deckId: this.activeDeck.deckId, cardId: this.currentCard.cardId, targetHeap: targetHeap.heapId, manualMove: true, nbAttempts: 0 });
                this.getCard();
            }
            this.showMoveToHeapMenuPopover = false;
            this.heapInfoPopover = false;
        },
        enqueueRatingUpload() {
            this.pendingRatingOperations.push({ cardId: this.currentCard.cardId, rating: this.currentCard.currentUserRating, nbAttempts: 0 });
        },
        timeToExitPage() {
            let result = !this.currentCard;
            result = result && this.downloadedCards.length === 0;
            result = result && this.lastDownloadIsEmpty;
            result = result && this.canExitPageSafely();
            return result;
        },
        canExitPageSafely() {
            let result = this.pendingMoveOperations.length === 0;
            result = result && !this.currentMovePromise;
            result = result && this.pendingRatingOperations.length === 0;
            result = result && !this.currentRatingPromise;
            result = result && this.pendingNotificationRegistrations.length === 0;
            result = result && !this.currentNotificationRegistrationPromise;
            return result;
        },
        handlePendingRatingOperations() {
            if (!this.currentRatingPromise && this.pendingRatingOperations.length > 0) {
                const ratingOperation = this.pendingRatingOperations.shift();

                if (this.demoMode()) {
                    if (this.timeToExitPage())
                        window.location.href = '/';
                    return;
                }

                const url = `/Learn/SetCardRating/${ratingOperation.cardId}/${ratingOperation.rating}`;
                const timeOut = Math.min(60000, (ratingOperation.nbAttempts + 1) * 1000);

                this.currentRatingPromise = pachAxios(url, timeOut)
                    .then(() => {
                        this.currentRatingPromise = null;
                        if (this.timeToExitPage())
                            window.location.href = '/';
                    })
                    .catch(() => {
                        sleep(1000).then(() => {
                            this.currentRatingPromise = null;
                            this.pendingRatingOperations.push({ cardId: ratingOperation.cardId, rating: ratingOperation.rating, nbAttempts: ratingOperation.nbAttempts + 1 });
                        });
                    });
            }
        },
        onImageClickFunctionText() {
            return 'const div = document.querySelector("#LearnMainDiv"); const thisApp = div.__vue_app__; const imageClicked = thisApp._component.methods.showImageFull; ';
        },
        currentCardFrontSide() {
            return convertMarkdown(this.currentCard.frontSide, this.currentCard.isInFrench, this.currentCard.images, this.onImageClickFunctionText());
        },
        currentCardBackSide() {
            return convertMarkdown(this.currentCard.backSide, this.currentCard.isInFrench, this.currentCard.images, this.onImageClickFunctionText());
        },
        currentCardAdditionalInfo() {
            return convertMarkdown(this.currentCard.additionalInfo, this.currentCard.isInFrench, this.currentCard.images, this.onImageClickFunctionText());
        },
        currentCardReferences() {
            return convertMarkdown(this.currentCard.references, this.currentCard.isInFrench, this.currentCard.images, this.onImageClickFunctionText());
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
                const operation = this.pendingNotificationRegistrations.shift();

                this.currentNotificationRegistrationPromise = axios.patch(`/Learn/SetCardNotificationRegistration/${operation.cardId}/${operation.notify}`)
                    .then(() => {
                        this.currentNotificationRegistrationPromise = null;
                        if (this.timeToExitPage())
                            window.location.href = '/';
                    })
                    .catch(() => {
                        sleep(1000).then(() => {
                            this.currentNotificationRegistrationPromise = null;
                            this.pendingNotificationRegistrations.push(operation);
                        });
                    });
            }
        },
        bigSizeImageLabelsLocalizer() {
            return localized;
        },
        onRatingChange(newValue) {
            this.enqueueRatingUpload(newValue);
        },
        updateRemainingCardsInLesson() {
            if (!this.activeDeck) {
                this.remainingCardsInLesson = this.downloadedCards.length;
                return;
            }
            const query = {
                deckId: this.activeDeck.deckId,
                learnModeIsUnknown: this.learningUnknowns(),
            };

            axios.post('/Learn/GetRemainingCardsInLesson', query)
                .then(result => {
                    this.remainingCardsInLesson = result.data.remainingCardsInLesson;
                })
                .catch(() => { });
        },
        offerDeckChoice() {
            return !this.demoMode() && !this.singleDeckDisplay;
        },
        getLessonTitle() {
            if (this.rehearsing())
                return localized.Rehearsing;
            if (this.learningUnknowns())
                return localized.Learning;
            if (this.demoMode())
                return localized.Demo;
            return `ERROR ${this.learnMode}`;
        },
        showEditButton() {
            return !this.demoMode();
        },
        showRemoveButton() {
            return !this.demoMode();
        },
        showFilteringButton() {
            return !this.demoMode();
        },
        showLastLearnDate() {
            return !this.demoMode() && isValidDateTime(this.currentCard.lastLearnUtcTime);
        },
        showDateAddedInDeck() {
            return !this.demoMode();
        },
        showNotificationRegistration() {
            return !this.demoMode();
        },
        showTimesInNotLearnedHeap() {
            return !this.demoMode();
        },
        showBiggestHeapReached() {
            return !this.demoMode();
        },
        showMoveToHeapMenu() {
            return !this.demoMode();
        },
        showRatingReadonly() {
            return this.demoMode();
        },
    },
    watch: {
        pendingMoveOperations: {
            handler: function pendingMoveOperationsHandler() {
                this.handlePendingMoveOperations();
            },
            deep: true
        },
        currentMovePromise: {
            handler: function currentMovePromiseHandler() {
                this.handlePendingMoveOperations();
            },
        },
        downloadedCards: {
            handler: function downloadedCardsHandler() {
                if (this.demoMode() && this.downloadedCards.length >= maxCountOfCardsForDemo)
                    this.reachedMaxCountOfCardsForDemo = true;
                this.downloadCardsIfNeeded();
                this.downloadImagesIfNeeded();
            },
            deep: true
        },
        cardDownloadOperation: {
            handler: function cardDownloadOperationHandler() {
                if (!this.lastDownloadIsEmpty)
                    this.downloadCardsIfNeeded();
            },
        },
        currentImageLoadingPromise: {
            handler: function currentImageLoadingPromiseHandler() {
                this.downloadImagesIfNeeded();
            },
        },
        currentImageDetailsLoadingPromise: {
            handler: function currentImageDetailsLoadingPromiseHandler() {
                this.downloadImagesIfNeeded();
            },
        },
        pendingRatingOperations: {
            handler: function pendingRatingOperationsHandler() {
                this.handlePendingRatingOperations();
            },
            deep: true
        },
        currentRatingPromise: {
            handler: function currentRatingPromiseHandler() {
                this.handlePendingRatingOperations();
            },
        },
        pendingNotificationRegistrations: {
            handler: function pendingNotificationRegistrationsHandler() {
                this.handlePendingNotificationRegistrations();
            },
            deep: true
        },
        currentNotificationRegistrationPromise: {
            handler: function currentNotificationRegistrationPromiseHandler() {
                this.handlePendingNotificationRegistrations();
            },
        },
    },
});

const learnAppInstance = learnApp.mount('#LearnMainDiv');
