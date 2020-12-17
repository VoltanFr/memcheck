Vue.component('big-size-image', {
    props: ['image', 'labels'],
    template: `
        <div id="big-size-image">
            <div id="TitleAndSmallButtons" class="big-size-image-top-div">
                <ul class="big-size-image-top-ul">
                    <li class="big-size-image-top-li big-size-image-name">{{image.name}}</li>
                    <li class="big-size-image-top-li"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.copyToClipboardButtonTitle" v-on:click="copyToClipboard()"><i class="far fa-copy"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasEditListener()"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.editButtonTitle" v-on:click="$emit('edit')"><i class="fas fa-pen"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasVersionHistoryListener()"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.versionHistoryButtonTitle" v-on:click="$emit('versionhistory')"><i class="fas fa-history"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasRemoveListener()"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.removeButtonTitle" v-on:click="$emit('remove')"><i class="fas fa-trash-alt"></i></button></li>
                    <li class="big-size-image-top-li"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.closeButtonTitle" v-on:click="$emit('close')"><i class="far fa-times-circle"></i></button></li>
                </ul>
            </div>
            <div id="FullScreenImage" class="big-size-image-middle-div">
                <img class="big-size-image-img" :src="image.blob" />
            </div>
            <div id="Details" class="big-size-image-bottom-div">
                <ul>
                    <li><strong>{{labels.name}}</strong> {{image.name}}</li>
                    <li v-if="image.description"><strong>{{labels.description}}</strong> {{image.description}}</li>
                    <li v-if="image.source"><strong>{{labels.source}}</strong> {{image.source}}</li>
                    <li v-if="image.initialUploadUtcDate"><strong>{{labels.initialVersionCreatedOn}}</strong> {{dateTime(image.initialUploadUtcDate)}}</li>
                    <li v-if="image.initialVersionCreator"><strong>{{labels.initialVersionCreatedBy}}</strong> {{image.initialVersionCreator}}</li>
                    <li v-if="image.currentVersionUtcDate"><strong>{{labels.currentVersionCreatedOn}}</strong> {{dateTime(image.currentVersionUtcDate)}}</li>
                    <li v-if="image.currentVersionCreator"><strong>{{labels.currentVersionCreatedBy}}</strong> {{image.currentVersionCreator}}</li>
                    <li v-if="image.currentVersionDescription"><strong>{{labels.currentVersionDescription}}</strong> {{image.currentVersionDescription}}</li>
                    <li v-if="image.cardCount"><strong>{{labels.numberOfCards}}</strong> {{image.cardCount}}</li>
                    <li v-if="image.originalImageContentType"><strong>{{labels.originalImageContentType}}</strong> {{image.originalImageContentType}}</li>
                    <li v-if="image.originalImageSize"><strong>{{labels.originalImageSize}}</strong> {{image.originalImageSize}}</li>
                    <li v-if="image.smallSize"><strong>{{labels.smallSize}}</strong> {{image.smallSize}}</li>
                    <li v-if="image.mediumSize"><strong>{{labels.mediumSize}}</strong> {{image.mediumSize}}</li>
                    <li v-if="image.bigSize"><strong>{{labels.bigSize}}</strong> {{image.bigSize}}</li>
                </ul>
                <p><a target="_blank" rel="noopener noreferrer" v-bind:href="'/Media/FullScreen/?ImageId='+image.imageId">{{labels.downloadBiggestSize}}</a></p>
            </div>
        </div>
    `,
    methods: {
        copyToClipboard() {
            copyToClipboardAndToast(this.image.name, this.labels.copiedToClipboardToastTitleOnSuccess, this.labels.copiedToClipboardToastTitleOnFailure, this);
        },
        hasRemoveListener() {
            return this.$listeners && this.$listeners.remove;
        },
        hasEditListener() {
            return this.$listeners && this.$listeners.edit;
        },
        hasVersionHistoryListener() {
            return this.$listeners && this.$listeners.versionhistory;
        },
    },
})
