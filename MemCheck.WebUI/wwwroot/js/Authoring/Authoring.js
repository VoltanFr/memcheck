var app = new Vue({
    el: '#AuthoringMainDiv',
    data: {
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
            cardSavedOk: "",
            failure: "",
            sureCreateWithoutTag: "",
            ratingSavedOk: "",
            saved: ""
        },
        addToDeck: "",  //AuthoringController.DecksOfUserViewModel
        decksOfUser: [],    //AuthoringController.DecksOfUserViewModel
        imageToAddFront: "", //string (name of image)
        imageToAddBack: "",
        imageToAddAdditional: "",
        frontSideImageList: [],   //"MyImageType": {imageId: Guid, blob: base64 string, ownerName: string, name: string, description: string, source: string, size: int, contentType: string}
        backSideImageList: [],   //MyImageType
        additionalInfoImageList: [],   //MyImageType
        originalFrontSideImageList: [],   //"MyImageType": {imageId: Guid, blob: base64 string, ownerName: string, name: string, description: string, source: string, size: int, contentType: string}
        originalBackSideImageList: [],   //MyImageType
        originalAdditionalInfoImageList: [],   //MyImageType
        currentFullScreenImage: null,   //MyImageType
        saving: false,
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            task1 = this.getCurrentUser();
            task2 = this.getAllAvailableTags();
            task3 = this.getAllAvailableLanguages();
            task4 = this.getUsers();
            task5 = this.GetCardToEditFromPageParameter();
            task6 = this.GetGuiMessages();
            task6 = this.GetDecksOfUser();
            this.GetReturnUrlFromPageParameter();
            await Promise.all([task1, task2, task3, task4, task5, task6]);
            if (this.creatingNewCard)
                this.makePublic();
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
            user = (await axios.get('/Authoring/GetCurrentUser')).data;
            this.currentUser = { userId: user.userId, userName: user.userName };
            this.card.languageId = user.preferredCardCreationLanguageId;
        },
        async getUsers() {
            this.allAvailableUsers = (await axios.get('/Authoring/GetUsers')).data;
        },
        async GetDecksOfUser() {
            this.decksOfUser = (await axios.get('/Authoring/DecksOfUser')).data;
            if (this.decksOfUser.length == 1)
                this.addToDeck = this.decksOfUser[0];
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
                    tellAxiosError(error, this);
                });
        },
        async sendCard() {
            if (this.card.tags.length == 0)
                if (!confirm(this.guiMessages.sureCreateWithoutTag))
                    return;

            this.saving = true;

            try {
                postCard = {
                    FrontSide: this.card.frontSide,
                    FrontSideImageList: this.frontSideImageList.map(img => img.imageId),
                    BackSide: this.card.backSide,
                    BackSideImageList: this.backSideImageList.map(img => img.imageId),
                    AdditionalInfo: this.card.additionalInfo,
                    AdditionalInfoImageList: this.additionalInfoImageList.map(img => img.imageId),
                    LanguageId: this.card.languageId,
                    Tags: this.card.tags.map(tag => tag.tagId),
                    UsersWithVisibility: this.card.usersWithView.map(user => user.userId),
                    AddToDeck: this.addToDeck.deckId,
                    VersionDescription: this.card.versionDescription,
                };

                task = this.creatingNewCard
                    ? axios.post('/Authoring/CardsOfUser/', postCard)
                    : axios.put('/Authoring/UpdateCard/' + this.editingCardId, postCard);

                await task
                    .then(response => {
                        this.clearAll();
                        if (this.returnUrl)
                            window.location = this.returnUrl;
                        this.$bvToast.toast(this.guiMessages.cardSavedOk, {
                            title: this.guiMessages.saved,
                            variant: 'success',
                            toaster: 'b-toaster-top-center',
                            solid: true,
                            autoHideDelay: 5000,
                        });
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
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
            this.makePublic();
            this.creatingNewCard = true;
            this.CopyAllInfoToOriginalCard();
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
        removeTag(index) {
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
        removeUser(index) {
            this.card.usersWithView.splice(index, 1);
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
            cardId = document.getElementById("CardIdInput").value;
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
                    tellAxiosError(error, this);
                });

            for (var i = 0; i < images.length; i++)
                await this.loadImage(images[i].imageId, images[i].ownerName, images[i].name, images[i].description, images[i].source, images[i].size, images[i].contentType, images[i].cardSide);
        },
        classForAdditionalInfo() {
            if (this.card.additionalInfo || this.additionalInfoImageList.length > 0)
                return "show";
            else
                return "collapse";
        },
        classForImages() {
            if ((this.frontSideImageList.length > 0) || (this.backSideImageList.length > 0) || (this.additionalInfoImageList.length > 0))
                return "show";
            else
                return "collapse";
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
        async loadImage(imageId, ownerName, name, description, source, size, contentType, side) {
            await axios.get('/Learn/GetImage/' + imageId + "/2", { responseType: 'arraybuffer' })
                .then(result => {
                    var xml = '';
                    var bytes = new Uint8Array(result.data);
                    var len = bytes.byteLength;
                    for (var j = 0; j < len; j++)
                        xml += String.fromCharCode(bytes[j]);
                    base64 = 'data:' + contentType + ';base64,' + window.btoa(xml);
                    var img = {
                        imageId: imageId,
                        blob: base64,
                        ownerName: ownerName,
                        name: name,
                        description: description,
                        source: source,
                        size: size,
                        contentType: contentType
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
                });
        },
        async addImage(side) {  //1 = front side ; 2 = back side ; 3 = AdditionalInfo
            switch (side) {
                case 1:
                    request = { imageName: this.imageToAddFront };
                    break;
                case 2:
                    request = { imageName: this.imageToAddBack };
                    break;
                case 3:
                    request = { imageName: this.imageToAddAdditional };
                    break;
            }

            await axios.post('/Authoring/GetImageInfo/', request)
                .then((getImageInfoResult) => {
                    if (this.imageIsInCard(getImageInfoResult.data.imageId)) {
                        this.$bvToast.toast("This image is already in the list", {
                            title: "Error",
                            variant: 'danger',
                            toaster: 'b-toaster-top-center',
                            solid: false,
                            autoHideDelay: 10000,
                        });
                        return;
                    }

                    this.loadImage(
                        getImageInfoResult.data.imageId,
                        getImageInfoResult.data.ownerName,
                        getImageInfoResult.data.name,
                        getImageInfoResult.data.description,
                        getImageInfoResult.data.source,
                        getImageInfoResult.data.size,
                        getImageInfoResult.data.contentType,
                        side
                    );
                })
                .catch(error => {
                    tellAxiosError(error, this);
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
                    tellAxiosError(err, this);
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
        frontSideHtml() {
            return convertMarkdown(this.card.frontSide);
        },
        backSideHtml() {
            return convertMarkdown(this.card.backSide);
        },
        additionalInfoHtml() {
            return convertMarkdown(this.card.additionalInfo);
        },
        currentUserRatingAsStars() {
            return ratingAsStars(this.card.currentUserRating);
        },
        averageRatingAsStars() {
            const truncated = Math.trunc(this.card.averageRating);
            return ratingAsStars(truncated);
        },
        async updateRating() {
            await axios.patch('/Authoring/SetCardRating/' + this.editingCardId + '/' + this.card.currentUserRating)
                .then(response => {
                    this.$bvToast.toast(this.guiMessages.ratingSavedOk, {
                        title: this.guiMessages.saved,
                        variant: 'success',
                        toaster: 'b-toaster-top-center',
                        solid: true,
                        autoHideDelay: 1000,
                    });
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
    watch: {
        selectedTagToAdd: {
            handler() {
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
