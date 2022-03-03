import * as vant from '../../lib/vant/vant.js';

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
                frontSide: "",
                backSide: "",
                additionalInfo: "",
                languageId: "", //Guid
                tags: [],   //AuthoringController.GetAllAvailableTagsViewModel
                usersWithView: [], //AuthoringController.GetUsersViewModel
                versionDescription: "",
                currentUserRating: 0,
                averageRating: 0,
                countOfUserRatings: 0,
            },
            originalCard: {
                frontSide: "",
                backSide: "",
                additionalInfo: "",
                languageId: "", //Guid
                tags: [],   //AuthoringController.GetAllAvailableTagsViewModel
                usersWithView: [] //AuthoringController.GetUsersViewModel
            },
            allAvailableTags: [],   //AuthoringController.GetAllAvailableTagsViewModel
            allAvailableLanguages: [],  //GetAllLanguages.ViewModel
            allAvailableUsers: [],  //AuthoringController.GetUsersViewModel
            selectedTagToAdd: "",   //AuthoringController.GetAllAvailableTagsViewModel
            selectedUserToAdd: "",  //AuthoringController.GetUsersViewModel
            currentUser: "", //AuthoringController.GetUsersViewModel
            creatingNewCard: true,  //If false, we are editing an existing card, see method GetCardToEditFromPageParameter
            editingCardId: "",  //Guid, used only if !creatingNewCard
            editingCardCreationDate: "",  //string, used only if !creatingNewCard
            editingCardLastChangeDate: "",  //string, used only if !creatingNewCard
            infoAboutUsage: "",  //string, used only if !creatingNewCard
            returnUrl: "", //string
            mountFinished: false,
            guiMessages: {
                success: "",
                failure: "",
                sureCreateWithoutTag: "",
            },
            addToDeck: "",  //AuthoringController.DecksOfUserViewModel
            addToSingleDeck: true,  //meaningful only if singleDeckDisplay
            decksOfUser: [],    //AuthoringController.DecksOfUserViewModel
            singleDeckDisplay: false,
            imageToAddFront: "", //string (name of image)
            imageToAddBack: "",
            imageToAddAdditional: "",
            frontSideImageList: [],   //"MyImageType": see loadImage
            backSideImageList: [],   //MyImageType
            additionalInfoImageList: [],   //MyImageType
            originalFrontSideImageList: [],   //MyImageType
            originalBackSideImageList: [],   //MyImageType
            originalAdditionalInfoImageList: [],   //MyImageType
            currentFullScreenImage: null,   //MyImageType
            saving: false,
            bigSizeImageLabels: null,   //MediaController.GetBigSizeImageLabels
            showInfoPopover: false,
            showAdditionalInfo: false,
        }
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            const task1 = this.getCurrentUser();
            const task2 = this.getAllAvailableTags();
            const task3 = this.getAllAvailableLanguages();
            const task4 = this.getUsers();
            const task5 = this.GetCardToEditFromPageParameter();
            const task6 = this.GetGuiMessages();
            const task7 = this.GetDecksOfUser();
            const task8 = this.GetBigSizeImageLabels();
            this.GetReturnUrlFromPageParameter();
            await Promise.all([task1, task2, task3, task4, task5, task6, task7, task8]);
            if (this.creatingNewCard)
                this.makePrivate();
            this.CopyAllInfoToOriginalCard();
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
        async getCurrentUser() {
            const user = (await axios.get('/Authoring/GetCurrentUser')).data;
            this.currentUser = { userId: user.userId, userName: user.userName };
            this.card.languageId = user.preferredCardCreationLanguageId;
        },
        async getUsers() {
            this.allAvailableUsers = (await axios.get('/Authoring/GetUsers')).data;
        },
        async GetDecksOfUser() {
            this.decksOfUser = (await axios.get('/Authoring/DecksOfUser')).data;
            if (this.decksOfUser.length == 1) {
                this.addToDeck = this.decksOfUser[0];
                this.singleDeckDisplay = true;
            }
            this.decksOfUser.splice(0, 0, "");
        },
        async getAllAvailableTags() {
            this.allAvailableTags = (await axios.get('/Authoring/AllAvailableTags')).data;
        },
        async getAllAvailableLanguages() {
            this.allAvailableLanguages = (await axios.get('/Languages/GetAllLanguages')).data;
        },
        async GetGuiMessages() {
            await axios.get('/Authoring/GetGuiMessages')
                .then(result => {
                    this.guiMessages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async GetBigSizeImageLabels() {
            await axios.get('/Media/GetBigSizeImageLabels')
                .then(result => {
                    this.bigSizeImageLabels = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async sendCard() {
            if (this.card.tags.length == 0)
                if (!confirm(this.guiMessages.sureCreateWithoutTag))
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
                    AdditionalInfoImageList: this.additionalInfoImageList.map(img => img.imageId),
                    LanguageId: this.card.languageId,
                    Tags: this.card.tags.map(tag => tag.tagId),
                    UsersWithVisibility: this.card.usersWithView.map(user => user.userId),
                    AddToDeck: deckToAddTo,
                    VersionDescription: this.card.versionDescription,
                };

                const task = this.creatingNewCard
                    ? axios.post('/Authoring/CardsOfUser/', postCard)
                    : axios.put('/Authoring/UpdateCard/' + this.editingCardId, postCard);

                await task
                    .then(result => {
                        this.clearAll();
                        tellControllerSuccess(result);
                        if (this.returnUrl)
                            window.location = this.returnUrl;
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
            this.card.frontSide = "";
            this.card.backSide = "";
            this.card.additionalInfo = "";
            this.card.tags = [];
            this.frontSideImageList = [];
            this.backSideImageList = [];
            this.additionalInfoImageList = [];
            this.makePrivate();
            this.creatingNewCard = true;
            this.CopyAllInfoToOriginalCard();
            this.showAdditionalInfo = false;
        },
        CopyAllInfoToOriginalCard() {
            this.originalCard.frontSide = this.card.frontSide;
            this.originalCard.backSide = this.card.backSide;
            this.originalCard.additionalInfo = this.card.additionalInfo;
            this.originalCard.languageId = this.card.languageId;
            this.originalCard.tags = this.card.tags.slice();
            this.originalCard.usersWithView = this.card.usersWithView.slice();
            this.originalFrontSideImageList = this.frontSideImageList.slice();
            this.originalBackSideImageList = this.backSideImageList.slice();
            this.originalAdditionalInfoImageList = this.additionalInfoImageList.slice();
        },
        cardContainsTag(tagId) {
            return this.card.tags.some(t => t.tagId == tagId);
        },
        addTag() {
            if (this.CanAddSelectedTag()) {
                this.card.tags.push(this.selectedTagToAdd);
                sortTagArray(this.card.tags);
            }
        },
        CanAddSelectedTag() {
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
            return this.card.usersWithView.some(user => user.userId == userId);
        },
        addUser() {
            if (this.CanAddSelectedUser()) {
                this.card.usersWithView.push(this.selectedUserToAdd);

                if (!this.cardContainsUserWithView(this.currentUser.userId))
                    this.card.usersWithView.push(this.currentUser);
            }
        },
        CanAddSelectedUser() {
            return this.selectedUserToAdd && !this.cardContainsUserWithView(this.selectedUserToAdd.userId);
        },
        showImageFull(image) {  //MyImageType
            this.currentFullScreenImage = image;
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
        async GetReturnUrlFromPageParameter() {
            this.returnUrl = document.getElementById("ReturnUrlInput").value;
        },
        async GetCardToEditFromPageParameter() {
            //There has to be a better way, but here's how I get a parameter passed to a page
            const cardId = document.getElementById("CardIdInput").value;
            if (!cardId) {
                this.creatingNewCard = true;
                return;
            }

            var images = [];

            await axios.get('/Authoring/GetCardForEdit/' + cardId)
                .then(result => {
                    this.editingCardId = cardId;
                    this.card.frontSide = result.data.frontSide;
                    this.card.backSide = result.data.backSide;
                    this.card.additionalInfo = result.data.additionalInfo;
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
                    tellAxiosError(error);
                });

            for (var i = 0; i < images.length; i++)
                await this.loadImage(images[i].imageId, images[i].name, images[i].source, images[i].cardSide);

            this.showAdditionalInfo = this.card.additionalInfo != "" || this.additionalInfoImageList.length > 0;
        },
        imageIsInCard(imageId) {
            for (var i = 0; i < this.frontSideImageList.length; i++)
                if (this.frontSideImageList[i].imageId == imageId)
                    return true;
            for (var i = 0; i < this.backSideImageList.length; i++)
                if (this.backSideImageList[i].imageId == imageId)
                    return true;
            for (var i = 0; i < this.additionalInfoImageList.length; i++)
                if (this.additionalInfoImageList[i].imageId == imageId)
                    return true;
            return false;
        },
        async loadImage(imageId, name, source, side) {
            await axios.get('/Learn/GetImage/' + imageId + "/2", { responseType: 'arraybuffer' })
                .then(result => {
                    var xml = '';
                    var bytes = new Uint8Array(result.data);
                    var len = bytes.byteLength;
                    for (var j = 0; j < len; j++)
                        xml += String.fromCharCode(bytes[j]);
                    const base64 = 'data:image/jpeg;base64,' + window.btoa(xml);
                    const img = {
                        imageId: imageId,
                        blob: base64,
                        name: name,
                        source: source,
                    };
                    switch (side) {
                        case 1:
                            this.frontSideImageList.push(img);
                            break;
                        case 2:
                            this.backSideImageList.push(img);
                            break;
                        case 3:
                            this.additionalInfoImageList.push(img);
                            break;
                    }
                })
                .catch(error => {
                    tellAxiosError("Failed to load image");
                });
        },
        async addFrontImage(imageName) {
            await this.addImage(imageName, 1);
        },
        async addBackImage(imageName) {
            await this.addImage(imageName, 2);
        },
        async addAdditionalImage(imageName) {
            await this.addImage(imageName, 3);
        },
        async addImage(imageName, side) {  //1 = front side ; 2 = back side ; 3 = AdditionalInfo
            await axios.post('/Authoring/GetImageInfo/', { imageName: imageName })
                .then((getImageInfoResult) => {
                    if (this.imageIsInCard(getImageInfoResult.data.imageId)) {
                        tellAxiosError("This image is already in the list");
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
        async pasteImageName(side) {  //1 = front side ; 2 = back side ; 3 = AdditionalInfo
            await navigator.clipboard.readText()
                .then(text => {
                    if (!text)
                        return;

                    switch (side) {
                        case 1:
                            this.imageToAddFront = text;
                            break;
                        case 2:
                            this.imageToAddBack = text;
                            break;
                        case 3:
                            this.imageToAddAdditional = text;
                            break;
                    }

                    this.addImage(side);
                })
                .catch(err => {
                    tellAxiosError(err);
                });
        },
        onBeforeUnload(event) {
            if (this.isDirty()) {
                (event || window.event).returnValue = "Sure you want to lose your edits?";
                return "Sure you want to lose your edits?";   //Message will not display on modern browers, but a fixed message will be displayed
            }
        },
        isDirty() {
            var result = this.card.frontSide != this.originalCard.frontSide;
            result = result || (this.card.backSide != this.originalCard.backSide);
            result = result || (this.card.additionalInfo != this.originalCard.additionalInfo);
            result = result || (this.card.languageId != this.originalCard.languageId);
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

            for (var id of setA)
                if (!setB.has(id))
                    return false;
            return true;
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
            return (this.currentUser.userName == "Voltan") || (this.currentUser.userName == "Toto1");
        },
        cardHistory() {
            window.location.href = "/Authoring/History?CardId=" + this.editingCardId;
        },
        isInFrench() {
            for (var i = 0; i < this.allAvailableLanguages.length; i++)
                if (this.allAvailableLanguages[i].id == this.card.languageId) {
                    var currentLanguageName = this.allAvailableLanguages[i].name;
                    var result = currentLanguageName.startsWith('Fr'); //This hardcoding is questionable
                    return result;
                }
            return false;
        },
        async updateRating(newValue) {
            await axios.patch('/Authoring/SetCardRating/' + this.editingCardId + '/' + newValue)
                .then(response => {
                    tellControllerSuccess(response);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        toggleShowAdditionalInfo() {
            this.showAdditionalInfo = !this.showAdditionalInfo;
        },
        async onRatingChange(newValue, oldValue) {
            await this.updateRating(newValue);
        }
    },
    watch: {
        selectedTagToAdd: {
            handler: function (newValue) {
                this.addTag();
                this.selectedTagToAdd = "";
            },
        },
        selectedUserToAdd: {
            handler() {
                this.addUser();
                this.selectedUserToAdd = "";
            },
        },
    },
});

authoringApp.mount('#AuthoringMainDiv');
