import { MarkdownEditor } from '../MarkdownEditor.js';
import { BigSizeImage } from '../big-size-image.js';
import { TagButton } from '../TagButton.js';
import { ImageChoice } from '../ImageChoice.js';
import { CardRating } from '../CardRating.js';
import { dateTime } from '../Common.js';
import { imageSizeMedium } from '../Common.js';
import { imageSideFront } from '../Common.js';
import { imageSideBack } from '../Common.js';
import { imageSideAdditional } from '../Common.js';
import { toast } from '../Common.js';
import { tellAxiosError } from '../Common.js';
import { tellControllerSuccess } from '../Common.js';
import { sortTagArray } from '../Common.js';

const authoringApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
        'van-popover': globalThis.vant.Popover,
        'van-rate': globalThis.vant.Rate,
        'markdown-editor': MarkdownEditor,
        'big-size-image': BigSizeImage,
        'card-rating': CardRating,
        'image-choice': ImageChoice,
        'tag-button': TagButton,
    },
    data() {
        return {
            card: {
                frontSide: '',
                backSide: '',
                additionalInfo: '',
                references: '',
                languageId: '', // Guid
                tags: [],   // AuthoringController.GetAllAvailableTagsViewModel
                usersWithView: [], // AuthoringController.GetUsersViewModel
                versionDescription: '',
                currentUserRating: 0,
                averageRating: 0,
                countOfUserRatings: 0,
            },
            originalCard: {
                frontSide: '',
                backSide: '',
                additionalInfo: '',
                references: '',
                languageId: '', // Guid
                tags: [],   // AuthoringController.GetAllAvailableTagsViewModel
                usersWithView: [] // AuthoringController.GetUsersViewModel
            },
            allAvailableTags: [],   // AuthoringController.GetAllAvailableTagsViewModel
            allAvailableLanguages: [],  // GetAllLanguages.ViewModel
            allAvailableUsers: [],  // AuthoringController.GetUsersViewModel
            selectedTagToAdd: '',   // AuthoringController.GetAllAvailableTagsViewModel
            selectedUserToAdd: '',  // AuthoringController.GetUsersViewModel
            currentUser: '', // { userId: Guid, userName: string }, set by getCurrentUser and never changed
            userPreferredCardCreationLanguageId: null, // set by getCurrentUser and never changed
            creatingNewCard: true,  // If false, we are editing an existing card, see method getCardToEditFromPageParameter
            editingCardId: '',  // Guid, used only if !creatingNewCard
            editingCardCreationDate: '',  // string, used only if !creatingNewCard
            editingCardLastChangeDate: '',  // string, used only if !creatingNewCard
            infoAboutUsage: '',  // string, used only if !creatingNewCard
            returnAddress: '', // string
            mountFinished: false,
            addToDeck: '',  // AuthoringController.DecksOfUserViewModel
            addToSingleDeck: true,  // meaningful only if singleDeckDisplay
            decksOfUser: [],    // AuthoringController.DecksOfUserViewModel
            singleDeckDisplay: false,
            imageToAddFront: '', // string (name of image)
            imageToAddBack: '',
            imageToAddAdditional: '',
            frontSideImageList: [],   // 'MyImageType': see loadImage
            backSideImageList: [],   // MyImageType
            additionalInfoImageList: [],   // MyImageType
            originalFrontSideImageList: [],   // MyImageType
            originalBackSideImageList: [],   // MyImageType
            originalAdditionalInfoImageList: [],   // MyImageType
            currentFullScreenImage: null,   // MyImageType
            saving: false,
            showInfoPopover: false,
            errorDebugInfoLines: [], // strings
        };
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            const task1 = this.getCurrentUser();
            const task2 = this.getAllAvailableTags();
            const task3 = this.getAllAvailableLanguages();
            const task4 = this.getUsers();
            const task5 = this.getCardToEditFromPageParameter();
            const task6 = this.getDecksOfUser();
            this.getReturnAddressFromPageParameter();
            await Promise.all([task1, task2, task3, task4, task5, task6]);
            if (this.initializationFailure())
                return;
            if (this.creatingNewCard) {
                this.makePrivate();
                this.card.languageId = this.userPreferredCardCreationLanguageId;
            }
            if (this.decksOfUser.length === 1) {
                this.addToDeck = this.decksOfUser[0];
                this.singleDeckDisplay = true;
            }
            this.decksOfUser.splice(0, 0, ''); // So that no deck is selected by default
            this.copyAllInfoToOriginalCard();
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
        async getCurrentUser() {
            await axios.get('/Authoring/GetCurrentUser')
                .then(result => {
                    this.currentUser = { userId: result.data.userId, userName: result.data.userName };
                    this.userPreferredCardCreationLanguageId = result.data.preferredCardCreationLanguageId;
                })
                .catch(error => {
                    this.errorDebugInfoLines.push(`Failed to get user info: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetUserInfo}`);
                    this.currentUser = null;
                });
        },
        async getUsers() {
            await axios.get('/Authoring/GetUsers')
                .then(result => {
                    this.allAvailableUsers = result.data;
                })
                .catch(error => {
                    this.errorDebugInfoLines.push(`Failed to get users: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetUsers}`);
                    this.allAvailableUsers = null;
                });
        },
        async getDecksOfUser() {
            await axios.get('/Authoring/DecksOfUser')
                .then(result => {
                    this.decksOfUser = result.data;
                })
                .catch(error => {
                    this.errorDebugInfoLines.push(`Failed to get decks of user: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetDecksOfUser}`);
                    this.decksOfUser = null;
                });
        },
        async getAllAvailableTags() {
            await axios.get('/Authoring/AllAvailableTags')
                .then(result => {
                    this.allAvailableTags = result.data;
                })
                .catch(error => {
                    this.errorDebugInfoLines.push(`Failed to get tags: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetTags}`);
                    this.allAvailableTags = null;
                });
        },
        async getAllAvailableLanguages() {
            await axios.get('/Languages/GetAllLanguages')
                .then(result => {
                    this.allAvailableLanguages = result.data;
                })
                .catch(error => {
                    this.errorDebugInfoLines.push(`Failed to get languages: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetLanguages}`);
                    this.allAvailableLanguages = null;
                });
        },
        initializationFailure() {
            return !this.currentUser || !this.allAvailableTags || !this.allAvailableLanguages || !this.allAvailableUsers || !this.decksOfUser;
        },
        bigSizeImageLabelsLocalizer() {
            return localized;
        },
        async sendCard() {
            if (this.card.tags.length === 0)
                if (!confirm(localized.SureCreateWithoutTag))
                    return;

            this.saving = true;

            try {
                const deckToAddTo = this.singleDeckDisplay ? (this.addToSingleDeck ? this.decksOfUser[1].deckId : undefined) : this.addToDeck.deckId;
                const postCard = {
                    FrontSide: this.card.frontSide,
                    FrontSideImageList: this.frontSideImageList.map(img => img.imageId),
                    BackSide: this.card.backSide,
                    BackSideImageList: this.backSideImageList.map(img => img.imageId),
                    AdditionalInfo: this.card.additionalInfo,
                    References: this.card.references,
                    AdditionalInfoImageList: this.additionalInfoImageList.map(img => img.imageId),
                    LanguageId: this.card.languageId,
                    Tags: this.card.tags.map(tag => tag.tagId),
                    UsersWithVisibility: this.card.usersWithView.map(user => user.userId),
                    AddToDeck: deckToAddTo,
                    VersionDescription: this.card.versionDescription,
                };

                const task = this.creatingNewCard
                    ? axios.post('/Authoring/CardsOfUser/', postCard)
                    : axios.put(`/Authoring/UpdateCard/${this.editingCardId}`, postCard);

                await task
                    .then(result => {
                        this.clearAll();
                        tellControllerSuccess(result);
                        if (this.returnAddress)
                            window.location = this.returnAddress;
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });
            }
            finally {
                this.saving = false;
            }
        },
        clearAll() {
            this.card.frontSide = '';
            this.card.backSide = '';
            this.card.additionalInfo = '';
            this.card.references = '';
            this.card.tags = [];
            this.frontSideImageList = [];
            this.backSideImageList = [];
            this.additionalInfoImageList = [];
            this.makePrivate();
            this.creatingNewCard = true;
            this.copyAllInfoToOriginalCard();
        },
        copyAllInfoToOriginalCard() {
            this.originalCard.frontSide = this.card.frontSide;
            this.originalCard.backSide = this.card.backSide;
            this.originalCard.additionalInfo = this.card.additionalInfo;
            this.originalCard.references = this.card.references;
            this.originalCard.languageId = this.card.languageId;
            this.originalCard.tags = this.card.tags.slice();
            this.originalCard.usersWithView = this.card.usersWithView.slice();
            this.originalFrontSideImageList = this.frontSideImageList.slice();
            this.originalBackSideImageList = this.backSideImageList.slice();
            this.originalAdditionalInfoImageList = this.additionalInfoImageList.slice();
        },
        cardContainsTag(tagId) {
            return this.card.tags.some(t => t.tagId === tagId);
        },
        addTag() {
            if (this.canAddSelectedTag()) {
                this.card.tags.push(this.selectedTagToAdd);
                sortTagArray(this.card.tags);
            }
        },
        canAddSelectedTag() {
            return this.selectedTagToAdd && !this.cardContainsTag(this.selectedTagToAdd.tagId);
        },
        removeTag(tagId) {
            const index = this.card.tags.findIndex(tag => tag.tagId === tagId);
            if (index > -1) {
                this.card.tags.splice(index, 1);
            }
        },
        removeTagByIndex(index) {
            this.card.tags.splice(index, 1);
        },
        cardContainsUserWithView(userId) {
            return this.card.usersWithView.some(user => user.userId === userId);
        },
        addUser() {
            if (this.canAddSelectedUser()) {
                this.card.usersWithView.push(this.selectedUserToAdd);

                if (!this.cardContainsUserWithView(this.currentUser.userId))
                    this.card.usersWithView.push(this.currentUser);
            }
        },
        canAddSelectedUser() {
            return this.selectedUserToAdd && !this.cardContainsUserWithView(this.selectedUserToAdd.userId);
        },
        onImageClickFunctionText() {
            return 'const div = document.querySelector("#AuthoringMainDiv"); const thisApp=div.__vue_app__; const imageClicked=thisApp._component.methods.showImageFull;';
        },
        showImageFull(imageBase64) {
            const decoded = atob(imageBase64);
            const image = JSON.parse(decoded);
            authoringAppInstance.currentFullScreenImage = image;
            history.pushState('ShowingImageDetails', 'BackToCard');
        },
        removeImageFromArray(image, array) {
            const index = array.indexOf(image);
            if (index > -1)
                array.splice(index, 1);
        },
        removeFullScreenImageFromCard() {
            this.removeImageFromArray(this.currentFullScreenImage, this.frontSideImageList);
            this.removeImageFromArray(this.currentFullScreenImage, this.backSideImageList);
            this.removeImageFromArray(this.currentFullScreenImage, this.additionalInfoImageList);
            this.currentFullScreenImage = null;
        },
        removeUser(userId) {
            const index = this.card.usersWithView.findIndex(user => user.userId === userId);
            if (index > -1) {
                this.card.usersWithView.splice(index, 1);
            }
        },
        makePrivate() {
            this.card.usersWithView = [this.currentUser];
        },
        makePublic() {
            this.card.usersWithView = [];
        },
        async getReturnAddressFromPageParameter() {
            this.returnAddress = document.getElementById('ReturnAddressInput').value;
        },
        async getCardToEditFromPageParameter() {
            const cardId = document.getElementById('CardIdInput').value;
            if (!cardId) {
                this.creatingNewCard = true;
                return;
            }

            let images = [];

            await axios.get(`/Authoring/GetCardForEdit/${cardId}`)
                .then(result => {
                    this.editingCardId = cardId;
                    this.card.frontSide = result.data.frontSide;
                    this.card.backSide = result.data.backSide;
                    this.card.additionalInfo = result.data.additionalInfo;
                    this.card.references = result.data.references;
                    this.card.languageId = result.data.languageId;
                    this.card.tags = result.data.tags;
                    this.card.usersWithView = result.data.usersWithVisibility;
                    this.card.currentUserRating = result.data.currentUserRating;
                    this.card.averageRating = result.data.averageRating;
                    this.card.countOfUserRatings = result.data.countOfUserRatings;
                    this.editingCardCreationDate = dateTime(result.data.creationUtcDate);
                    this.editingCardLastChangeDate = dateTime(result.data.lastChangeUtcDate);
                    this.infoAboutUsage = result.data.infoAboutUsage;
                    this.creatingNewCard = false;
                    images = result.data.images;
                })
                .catch(error => {
                    this.clearAll();
                    this.errorDebugInfoLines.push(`Failed to get card to edit: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToGetCardToEdit}`);
                });

            for (let i = 0; i < images.length; i++)
                await this.loadImage(images[i].imageId, images[i].name, images[i].source, images[i].cardSide);
        },
        imageIsInCard(imageId) {
            for (let i = 0; i < this.frontSideImageList.length; i++)
                if (this.frontSideImageList[i].imageId === imageId)
                    return true;
            for (let i = 0; i < this.backSideImageList.length; i++)
                if (this.backSideImageList[i].imageId === imageId)
                    return true;
            for (let i = 0; i < this.additionalInfoImageList.length; i++)
                if (this.additionalInfoImageList[i].imageId === imageId)
                    return true;
            return false;
        },
        async loadImage(imageId, name, source, side) {
            await axios.get(`/Learn/GetImage/${imageId}/${imageSizeMedium}`, { responseType: 'arraybuffer' })
                .then(result => {
                    let xml = '';
                    const bytes = new Uint8Array(result.data);
                    for (let j = 0; j < bytes.byteLength; j++)
                        xml += String.fromCharCode(bytes[j]);
                    const base64 = `data:image/jpeg;base64,${window.btoa(xml)}`;
                    const img = {
                        imageId: imageId,
                        blob: base64,
                        name: name,
                        source: source,
                    };
                    switch (side) {
                        case imageSideFront:
                            this.frontSideImageList.push(img);
                            break;
                        case imageSideBack:
                            this.backSideImageList.push(img);
                            break;
                        case imageSideAdditional:
                            this.additionalInfoImageList.push(img);
                            break;
                        default:
                            return;
                    }
                })
                .catch(error => {
                    this.clearAll();
                    this.errorDebugInfoLines.push(`Failed to load image: ${error}`);
                    tellAxiosError(error, `${localized.NetworkError} - ${localized.FailedToLoadImage}`);
                });
        },
        async addFrontImage(imageName) {
            await this.addImage(imageName, imageSideFront);
        },
        async addBackImage(imageName) {
            await this.addImage(imageName, imageSideBack);
        },
        async addAdditionalImage(imageName) {
            await this.addImage(imageName, imageSideAdditional);
        },
        async addImage(imageName, side) {
            await axios.post('/Authoring/GetImageInfo/', { imageName: imageName })
                .then((getImageInfoResult) => {
                    if (this.imageIsInCard(getImageInfoResult.data.imageId)) {
                        toast(localized.ImageAlreadyInCard, localized.Failure, false);
                        return;
                    }

                    this.loadImage(
                        getImageInfoResult.data.imageId,
                        getImageInfoResult.data.name,
                        getImageInfoResult.data.source,
                        side
                    );
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async pasteImageName(side) {
            await navigator.clipboard.readText()
                .then(text => {
                    if (!text)
                        return;

                    switch (side) {
                        case imageSideFront:
                            this.imageToAddFront = text;
                            break;
                        case imageSideBack:
                            this.imageToAddBack = text;
                            break;
                        case imageSideAdditional:
                            this.imageToAddAdditional = text;
                            break;
                        default:
                            return;
                    }

                    this.addImage(side);
                })
                .catch(err => {
                    tellAxiosError(err);
                });
        },
        onBeforeUnload(event) {
            if (this.isDirty()) {
                (event || window.event).returnValue = 'Sure you want to lose your edits?';
                return 'Sure you want to lose your edits?';   // Message will not display on modern browers, but a fixed message will be displayed
            }
            return null;
        },
        isDirty() {
            let result = this.card.frontSide !== this.originalCard.frontSide;
            result = result || (this.card.backSide !== this.originalCard.backSide);
            result = result || (this.card.additionalInfo !== this.originalCard.additionalInfo);
            result = result || (this.card.references !== this.originalCard.references);
            result = result || (this.card.languageId !== this.originalCard.languageId);
            result = result || (!this.sameSetOfIds(this.card.tags.map(tag => tag.tagId), this.originalCard.tags.map(tag => tag.tagId)));
            result = result || (!this.sameSetOfIds(this.card.usersWithView.map(user => user.userId), this.originalCard.usersWithView.map(user => user.userId)));
            result = result || (!this.sameSetOfIds(this.frontSideImageList.map(img => img.imageId), this.originalFrontSideImageList.map(img => img.imageId)));
            result = result || (!this.sameSetOfIds(this.backSideImageList.map(img => img.imageId), this.originalBackSideImageList.map(img => img.imageId)));
            result = result || (!this.sameSetOfIds(this.additionalInfoImageList.map(img => img.imageId), this.originalAdditionalInfoImageList.map(img => img.imageId)));
            return result;
        },
        sameSetOfIds(arrayA, arrayB) {
            if (arrayA.length !== arrayB.length) return false;

            const setA = new Set(arrayA);
            const setB = new Set(arrayB);

            for (let id of setA)
                if (!setB.has(id))
                    return false;
            return true;
        },
        onPopState() {
            // If we are in full screen image mode, a state '#' has been pushed by the browser
            if (!document.location.href.endsWith('#'))
                this.currentFullScreenImage = null;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        showDebugInfo() {
            return this.currentUser && ((this.currentUser.userName === 'Voltan') || (this.currentUser.userName === 'Toto1'));
        },
        cardHistory() {
            window.location.href = `/Authoring/History?CardId=${this.editingCardId}`;
        },
        isInFrench() {
            for (let i = 0; i < this.allAvailableLanguages.length; i++)
                if (this.allAvailableLanguages[i].id === this.card.languageId) {
                    const currentLanguageName = this.allAvailableLanguages[i].name;
                    return currentLanguageName.startsWith('Fr'); // To be revisited, bad hardcoding
                }
            return false;
        },
        async updateRating(newValue) {
            await axios.patch(`/Authoring/SetCardRating/${this.editingCardId}/${newValue}`)
                .then(response => {
                    tellControllerSuccess(response);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async onRatingChange(newValue) {
            await this.updateRating(newValue);
        }
    },
    watch: {
        selectedTagToAdd: {
            handler: function selectedTagToAddHandler() {
                this.addTag();
                this.selectedTagToAdd = '';
            },
        },
        selectedUserToAdd: {
            handler() {
                this.addUser();
                this.selectedUserToAdd = '';
            },
        },
    },
});

const authoringAppInstance = authoringApp.mount('#AuthoringMainDiv');
