function dateTime(utcFromDotNet) {
    if (!utcFromDotNet || utcFromDotNet == "0001-01-01T00:00:00Z" || utcFromDotNet == "9999-12-31T23:59:59.9999999")  //matches DateTime.MinValue
        return "!";
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (dateIsTomorrow(d))
        return d.toLocaleString([], { year: "numeric", month: "numeric", day: "numeric", hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString();
}

function dateIsToday(d) {
    return new Date().toDateString() == d.toDateString();
}

function dateIsTomorrow(d) {
    const tomorrow = new Date(new Date().getTime() + (24 * 60 * 60 * 1000));
    return tomorrow.toDateString() == d.toDateString();
}

function tellAxiosError(error, vueObject) {
    console.log(error);
    var mesg = error.message;
    if (error.response && error.response.data && error.response.data.text)
        mesg = error.response.data.text + (error.response.data.showStatus ? ("\r\n" + mesg) : "");
    tellAxiosMsg(mesg, "FAILURE", 'danger', 10000, vueObject);
}

function tellAxiosSuccess(mesg, title, vueObject) {
    tellAxiosMsg(mesg, title, 'success', 3000, vueObject);
}

function tellAxiosMsg(mesg, title, variant, autoHideDelay, vueObject) {
    //variant: 'danger', 'success'
    vueObject.$bvToast.toast(mesg, {
        title: title,
        variant: variant,
        toaster: 'b-toaster-top-center',
        solid: false,
        autoHideDelay: autoHideDelay,
    });
}

function base64FromBytes(bytes) {
    var xml = '';
    var bytes = new Uint8Array(bytes);
    var len = bytes.byteLength;
    for (var j = 0; j < len; j++)
        xml += String.fromCharCode(bytes[j]);
    return 'data:image/jpg;base64,' + window.btoa(xml);
}

function sortTagArray(array) {
    array.sort((tagA, tagB) => (tagA.tagName > tagB.tagName) ? 1 : ((tagB.tagName > tagA.tagName) ? -1 : 0));
}

function sleep(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds))
}

function copyToClipboardAndToast(text, toastTitleOnSuccess, toastTitleOnFailure, vueObject) {
    navigator.clipboard.writeText(text).then(function () {
        tellAxiosSuccess(text, toastTitleOnSuccess, vueObject);
    }, function (err) {
        tellAxiosMsg(err, toastTitleOnFailure, 'danger', 10000, vueObject);
    });
}

function convertMarkdown(src) {
    var converter = new showdown.Converter({ tables: true });
    converter.setOption('openLinksInNewWindow', 'true');
    converter.setOption('simplifiedAutoLink', 'true');
    converter.setOption('simpleLineBreaks', 'true');
    var html = converter.makeHtml(src);
    return html;
}

function ratingAsStars(rating) {    //rating is an int
    var result = "";
    for (let i = 0; i < rating; i++)
        result = result + "\u2605";
    for (let i = 0; i < 5 - rating; i++)
        result = result + "\u2606";
    return result;
}
