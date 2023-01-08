import { dateTime } from './Common.js';
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
            <div id="FullScreenImage" class="big-size-image-middle-div">
                <img class="big-size-image-img" :src="image.blob" />
            </div>
            <div id="Details" class="big-size-image-bottom-div">
                <details>
                    <summary class="big-size-image-foldout-summary">
                        <span class="big-size-image-foldout-summary-caption">{{labellocalizer.BigSizeImageLabel_FoldoutCaption}}</span>
                        <button class="toolbar-button-circle toolbar-button dropdown-toggle" href="#" id="BigSizeImageCopyToClipboardDropdown" role="button" v-bind:title="labellocalizer.BigSizeImageLabel_CopyToClipboard" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="far fa-copy"></i></button>
                        <div class="dropdown-menu" role="menu" aria-labelledby="CopyToClipboardDropdown">
                            <a class="dropdown-item" v-on:click="copyToClipboard('![Mnesios:' + image.name + ',size=small]')">{{labellocalizer.ImageForCardSmallToClipboard}}</a>
                            <a class="dropdown-item" v-on:click="copyToClipboard('![Mnesios:' + image.name + ',size=medium]')">{{labellocalizer.ImageForCardMediumToClipboard}}</a>
                            <a class="dropdown-item" v-on:click="copyToClipboard('![Mnesios:' + image.name + ',size=big]')">{{labellocalizer.ImageForCardBigToClipboard}}</a>
                        </div>
                        <button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_EditButtonTitle" v-on:click="$emit('edit')" v-if="hasEditListener()""><i class="fas fa-pen"></i></button>
                        <button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_VersionHistoryButtonTitle" v-on:click="$emit('versionhistory')" v-if="hasVersionHistoryListener()"><i class="fas fa-history"></i></button>
                        <button class="toolbar-button-circle toolbar-button" v-bind:title="labellocalizer.BigSizeImageLabel_CloseButtonTitle" v-on:click="$emit('close')"><i class="far fa-times-circle"></i></button>
                    </summary>
                    <ul>
                        <li><strong>{{labellocalizer.BigSizeImageLabel_Name}}</strong> {{image.name}}</li>
                        <li v-if="image.description"><strong>{{labellocalizer.BigSizeImageLabel_Description}}</strong> {{image.description}}</li>
                        <li v-if="image.source"><strong>{{labellocalizer.BigSizeImageLabel_Source}}</strong> {{image.source}}</li>
                        <li v-if="image.initialUploadUtcDate"><strong>{{labellocalizer.BigSizeImageLabel_InitialVersionCreatedOn}}</strong> {{dateTime(image.initialUploadUtcDate)}}</li>
                        <li v-if="image.currentVersionCreator"><strong>{{labellocalizer.BigSizeImageLabel_CurrentVersionCreatedBy}}</strong> {{image.currentVersionCreator}}</li>
                        <li v-if="image.currentVersionUtcDate"><strong>{{labellocalizer.BigSizeImageLabel_CurrentVersionCreatedOn}}</strong> {{dateTime(image.currentVersionUtcDate)}}</li>
                        <li v-if="image.currentVersionDescription"><strong>{{labellocalizer.BigSizeImageLabel_CurrentVersionDescription}}</strong> {{image.currentVersionDescription}}</li>
                        <li v-if="image.cardCount || image.cardCount === 0"><strong>{{labellocalizer.BigSizeImageLabel_NumberOfCards}}</strong> {{image.cardCount}}</li>
                        <li v-if="image.originalImageContentType"><strong>{{labellocalizer.BigSizeImageLabel_originalImageContentType}}</strong> {{image.originalImageContentType}}</li>
                        <li v-if="image.originalImageSize"><strong>{{labellocalizer.BigSizeImageLabel_OriginalImageSize}}</strong> {{image.originalImageSize}}</li>
                        <li v-if="image.smallSize"><strong>{{labellocalizer.BigSizeImageLabel_SmallSize}}</strong> {{image.smallSize}}</li>
                        <li v-if="image.mediumSize"><strong>{{labellocalizer.BigSizeImageLabel_MediumSize}}</strong> {{image.mediumSize}}</li>
                        <li v-if="image.bigSize"><strong>{{labellocalizer.BigSizeImageLabel_BigSize}}</strong> {{image.bigSize}}</li>
                        <li v-if="image.imageId"><strong>{{labellocalizer.BigSizeImageLabel_URL}}</strong> <a v-bind:href="'/Media/Index/?ImageId='+image.imageId">https://www.mnesios.com/Media/Index/?ImageId={{image.imageId}}</a></li>
                    </ul>
                </details>
                <p><a target="_blank" rel="noopener noreferrer" v-bind:href="'/Media/FullScreen/?ImageId='+image.imageId">{{labellocalizer.BigSizeImageLabel_DownloadBiggestSize}}</a></p>
            </div>
        </div>
    `,
    methods: {
        copyToClipboard(text) {
            copyToClipboardAndToast(text, this.labellocalizer.BigSizeImageLabel_CopiedToClipboardToastTitleOnSuccess, this.labellocalizer.BigSizeImageLabel_CopiedToClipboardToastTitleOnFailure);
        },
        hasEditListener() {
            return this.$attrs && this.$attrs.onEdit;
        },
        hasVersionHistoryListener() {
            return this.$attrs && this.$attrs.onVersionhistory;
        },
    },
});
