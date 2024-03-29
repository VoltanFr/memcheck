﻿import { toastWithoutIcon } from './Common.js';

export const CardRating = Vue.defineComponent({
    components: {
        'van-button': globalThis.vant.Button,
        'van-popover': globalThis.vant.Popover,
        'van-rate': globalThis.vant.Rate,
    },
    props: {
        modelValue: { required: false, type: Number }, // Current user rating, can be null
        average: { required: true, type: Number },
        countinaverage: { required: false, type: Number },
        micro: { required: false, type: Boolean, default: false },
        readonly: { required: false, type: Boolean, default: false },
        yourratingstr: { required: true, type: String },
        averagestr: { required: true, type: String },
        usersstr: { required: true, type: String },
        userstr: { required: true, type: String },
        noevaluationstr: { required: true, type: String },
    },
    template: `
        <div class="rating-component-div" >
            <div v-if="micro">
                <van-button class="toolbar-button rating-button" v-on:click="onMicroButtonClick()" >
                    <i class='fas fa-star rating-star'></i> {{ Math.round(average * 10) / 10 }}
                </van-button>
            </div>
            <div v-else>
                <van-popover v-model:show="ratingPopover" overlay close-on-click-outside close-on-click-overlay >
                    <p class="rating-paragraph" >
                        <div v-if="modelValue != null">
                            {{yourratingstr}} <van-rate v-bind:modelValue="modelValue" v-bind:readonly="readonly" @change="onChange" color="black"></van-rate><br />
                        </div>
                        {{averagestr}}<span v-if="countinaverage"> ({{countinaverage}} <span v-if="countinaverage > 1">{{usersstr}}</span><span v-else>{{userstr}}</span>) :</span> <van-rate readonly color="black" v-model="average" allow-half v-bind:title="average"></van-rate>
                    </p>
                    <template #reference>
                        <van-button class="toolbar-button rating-button" >
                            <div v-if="modelValue != null">
                                <span v-html="currentUserRatingAsStars()" /><br />
                            </div>
                            <span v-html="averageRatingAsStars()" />                            
                        </van-button>
                    </template>
                </van-popover>
            </div>
        </div>
    `,
    data() {
        return {
            ratingPopover: false,
        };
    },
    methods: {
        onChange(newValue) {
            this.$emit('update:modelValue', newValue);
            this.ratingPopover = false;
        },
        ratingAsStars(rating) {
            let result = '';

            const truncated = Math.trunc(rating);
            for (let i = 0; i < truncated; i++)
                result = `${result}<i class='fas fa-star rating-star'></i>`;

            const ceil = Math.ceil(rating);
            for (let i = truncated; i < ceil; i++)
                result = `${result}<i class='fas fa-star-half-alt rating-star'></i>`;


            for (let i = ceil; i < 5; i++)
                result = `${result}<i class='far fa-star rating-star'></i>`;
            return result;
        },
        currentUserRatingAsStars() {
            return this.ratingAsStars(this.modelValue);
        },
        averageRatingAsStars() {
            return this.ratingAsStars(this.average);
        },
        onMicroButtonClick() {
            if (this.average === 0)
                toastWithoutIcon('', this.noevaluationstr);
            else
                toastWithoutIcon(`${Math.round(this.average * 100) / 100}\n${this.averageRatingAsStars()}`, this.averagestr);
        },
    },
});
