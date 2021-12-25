var app = new Vue({
    el: '#TagsMainDiv',
    data: {
        request: {
            filter: "", //string
            pageNo: 1, //int. First page is number 1
            pageSize: 100,   //int
        },
        totalTagCount: -1, //int
        pageCount: 0,   //int
        offeredPageSizes: [10, 50, 100, 500],
        tags: [],    //TagsController.GetTagsTagViewModel
        mountFinished: false,
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
            await axios.post("/Tags/GetTags", this.request)
                .then(result => {
                    this.totalTagCount = result.data.totalCount;
                    this.pageCount = result.data.pageCount;
                    this.tags = result.data.tags;
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
    },
});