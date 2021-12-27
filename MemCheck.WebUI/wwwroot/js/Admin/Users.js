const usersApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            request: {
                filter: "", //string
                pageNo: 1, //int. First page is number 1
                pageSize: 100,   //int
            },
            totalUserCount: -1, //int
            pageCount: 0,   //int
            offeredPageSizes: [10, 50, 100, 500],
            users: [],    //AdminController.GetUsersUserViewModel
            mountFinished: false,
        }
    },
    async mounted() {
        try {
            await this.getUsers();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getUsers() {
            this.users = [];
            await axios.post("/Admin/GetUsers", this.request)
                .then(result => {
                    this.totalTagCount = result.data.totalCount;
                    this.pageCount = result.data.pageCount;
                    this.users = result.data.users;
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
            await this.getUsers();
        },
        async moveToLastPage() {
            this.request.pageNo = this.pageCount;
            await this.getUsers();
        },
        async movePage(shift) {
            this.request.pageNo = this.request.pageNo + shift;
            await this.getUsers();
        },
    },
});

usersApp.mount('#UsersMainDiv');
