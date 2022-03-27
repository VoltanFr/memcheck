const tagListingApp = Vue.createApp({
    components: {
        'card-rating': CardRating,
    },
    data() {
        return {
            request: {
                filter: "", //string
                pageNo: 1, //int. First page is number 1
                pageSize: 100,   //int
            },
            totalTagCount: -1, //int
            pageCount: 0,   //int
            userLoggedIn: false,
            offeredPageSizes: [10, 50, 100, 500],
            tags: [],    //TagsController.GetTagsTagViewModel, but we add a "folded" boolean
            mountFinished: false,
        }
    },
    async mounted() {
        try {
            await this.getTags();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getTags() {
            this.tags = [];
            this.tagVisibility = [];
            await axios.post("/Tags/GetTags", this.request)
                .then(result => {
                    this.totalTagCount = result.data.totalCount;
                    this.pageCount = result.data.pageCount;
                    this.userLoggedIn = result.data.userLoggedIn;

                    for (var i = 0; i < result.data.tags.length; i++) {
                        const tagWithVisibility = {
                            tagId: result.data.tags[i].tagId,
                            tagName: result.data.tags[i].tagName,
                            tagDescription: result.data.tags[i].tagDescription,
                            cardCount: result.data.tags[i].cardCount,
                            averageRating: result.data.tags[i].averageRating,
                            folded: true
                        };
                        this.tags.push(tagWithVisibility);
                    }
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        canMovePage(shift) {
            return (this.request.pageNo + shift > 0) && (this.request.pageNo + shift <= this.pageCount);
        },
        async moveToFirstPage() {
            this.request.pageNo = 1;
            await this.getTags();
        },
        async moveToLastPage() {
            this.request.pageNo = this.pageCount;
            await this.getTags();
        },
        async movePage(shift) {
            this.request.pageNo = this.request.pageNo + shift;
            await this.getTags();
        },
        showCardsWithTag(tagId) {
            window.location.href = "/Search?TagFilter=" + tagId;
        },
        edit(tagId) {
            window.location.href = "/Tags/Authoring?TagId=" + tagId + "&ReturnUrl=" + window.location;
        },
        demo(tagId) {
            window.location.href = `/Learn/Index?LearnMode=Demo&TagId=${tagId}`;
        },
        tagMarkdownDescription(tag) {
            return convertMarkdown(tag.tagDescription, true);
        },
        tagIsUnfoldable(tag) {
            return tag.tagDescription.length > 0;
        },
        tagIsFolded(tag) {
            return tag.folded;
        },
        fold(tag) {
            tag.folded = true;
        },
        unfold(tag) {
            tag.folded = false;
        },
    },
});

tagListingApp.mount('#TagsMainDiv');
