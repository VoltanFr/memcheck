﻿import { dateTime } from './Common.js';
import { copyToClipboardAndToast } from './Common.js';

export const BigSizeImage = Vue.defineComponent({
    components: {
        // mention sub components, if we used in the template
    },
    props: {
        image: { required: true },
        labellocalizer: { required: true },
    },
    beforeCreate() {
        this.dateTime = dateTime;
    },
    template: `
        <div id="big-size-image">
            <div id="TitleAndSmallButtons" class="big-size-image-top-div">
                <ul class="big-size-image-top-ul">
                    <li class="big-size-image-top-li big-size-image-name">{{image.name}}</li>
                    <li class="big-size-image-top-li"><button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_CopyToClipboard" v-on:click="copyToClipboard()"><i class="far fa-copy"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasEditListener()"><button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_EditButtonTitle" v-on:click="$emit('edit')"><i class="fas fa-pen"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasVersionHistoryListener()"><button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_VersionHistoryButtonTitle" v-on:click="$emit('versionhistory')"><i class="fas fa-history"></i></button></li>
                    <li class="big-size-image-top-li" v-if="hasRemoveListener()"><button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_Remove" v-on:click="$emit('remove')"><i class="fas fa-trash-alt"></i></button></li>
                    <li class="big-size-image-top-li"><button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_CloseButtonTitle" v-on:click="$emit('close')"><i class="far fa-times-circle"></i></button></li>
                </ul>
            </div>
            <div id="FullScreenImage" class="big-size-image-middle-div">
                <img class="big-size-image-img" :src="image.blob" />
            </div>
            <div id="Details" class="big-size-image-bottom-div">
                <ul>
                    <li><strong>{{labellocalizer.BigSizeImageLabel_Name}}</strong> {{image.name}}</li>
                    <li v-if="image.description"><strong>{{labellocalizer.BigSizeImageLabel_Description}}</strong> {{image.description}}</li>
                    <li v-if="image.source"><strong>{{labellocalizer.BigSizeImageLabel_Source}}</strong> {{image.source}}</li>
                    <li v-if="image.initialUploadUtcDate"><strong>{{labellocalizer.BigSizeImageLabel_InitialVersionCreatedOn}}</strong> {{dateTime(image.initialUploadUtcDate)}}</li>
                    <li v-if="image.initialVersionCreator"><strong>{{labellocalizer.BigSizeImageLabel_InitialVersionCreatedBy}}</strong> {{image.initialVersionCreator}}</li>
                    <li v-if="image.currentVersionUtcDate"><strong>{{labellocalizer.BigSizeImageLabel_CurrentVersionCreatedOn}}</strong> {{dateTime(image.currentVersionUtcDate)}}</li>
                    <li v-if="image.currentVersionDescription"><strong>{{labellocalizer.BigSizeImageLabel_CurrentVersionDescription}}</strong> {{image.currentVersionDescription}}</li>
                    <li v-if="image.cardCount"><strong>{{labellocalizer.BigSizeImageLabel_NumberOfCards}}</strong> {{image.cardCount}}</li>
                    <li v-if="image.originalImageContentType"><strong>{{labellocalizer.BigSizeImageLabel_originalImageContentType}}</strong> {{image.originalImageContentType}}</li>
                    <li v-if="image.originalImageSize"><strong>{{labellocalizer.BigSizeImageLabel_OriginalImageSize}}</strong> {{image.originalImageSize}}</li>
                    <li v-if="image.smallSize"><strong>{{labellocalizer.BigSizeImageLabel_SmallSize}}</strong> {{image.smallSize}}</li>
                    <li v-if="image.mediumSize"><strong>{{labellocalizer.BigSizeImageLabel_MediumSize}}</strong> {{image.mediumSize}}</li>
                    <li v-if="image.bigSize"><strong>{{labellocalizer.BigSizeImageLabel_BigSize}}</strong> {{image.bigSize}}</li>
                </ul>
                <p><a target="_blank" rel="noopener noreferrer" v-bind:href="'/Media/FullScreen/?ImageId='+image.imageId">{{labellocalizer.BigSizeImageLabel_DownloadBiggestSize}}</a></p>
            </div>
        </div>
    `,
    methods: {
        copyToClipboard() {
            copyToClipboardAndToast(this.image.name, this.labellocalizer.BigSizeImageLabel_CopiedToClipboardToastTitleOnSuccess, this.labellocalizer.BigSizeImageLabel_CopiedToClipboardToastTitleOnFailure);
        },
        hasRemoveListener() {
            return this.$attrs && this.$attrs.onRemove;
        },
        hasEditListener() {
            return this.$attrs && this.$attrs.onEdit;
        },
        hasVersionHistoryListener() {
            return this.$attrs && this.$attrs.onVersionhistory;
        },
    },
});
