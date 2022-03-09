const tagListingApp = Vue.createApp({
    components: {
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

                    for (var i = 0; i < result.data.tags.length; i++) {
                        const tagWithVisibility = {
                            tagId: result.data.tags[i].tagId,
                            tagName: result.data.tags[i].tagName,
                            tagDescription: result.data.tags[i].tagDescription,
                            cardCount: result.data.tags[i].cardCount,
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
