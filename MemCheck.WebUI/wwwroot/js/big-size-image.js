Vue.component('big-size-image', {
    props: ['image', 'labels'],
    template: `
        <div id="big-size-image">
            <div id="TitleAndSmallButtons" class="big-size-image-top-div">
                <ul class="big-size-image-top-ul">
                    <li class="big-size-image-top-li big-size-image-name">{{image.name}}</li>
                    <li class="big-size-image-top-li"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.removeButtonTitle" v-on:click="$emit('remove')"><i class="fas fa-trash-alt"></i></button></li>
                    <li class="big-size-image-top-li"><button class="btn btn-primary btn-circle btn-sm" v-bind:title="labels.closeButtonTitle" v-on:click="$emit('close')"><i class="far fa-times-circle"></i></button></li>
                </ul>
            </div>
            <div id="FullScreenImage" class="big-size-image-middle-div">
                <img class="big-size-image-img" :src="image.blob" />
            </div>
            <div id="Details" class="big-size-image-bottom-div">
                <ul>
                    <li><strong>{{labels.name}}</strong> {{image.name}}</li>
                    <li><strong>{{labels.uploaderName}}</strong> {{image.ownerName}}</li>
                    <li><strong>{{labels.description}}</strong> {{image.description}}</li>
                    <li><strong>{{labels.source}}</strong> {{image.source}}</li>
                    <li><strong>{{labels.size}}</strong> {{image.size}}</li>
                    <li><strong>{{labels.type}}</strong> {{image.contentType}}</li>
                </ul>
            </div>
        </div>
    `
})