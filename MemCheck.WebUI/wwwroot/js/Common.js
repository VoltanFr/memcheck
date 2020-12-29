function isValidDateTime(utcFromDotNet) {
    return utcFromDotNet && (utcFromDotNet != "0001-01-01T00:00:00Z") && (utcFromDotNet != "9999-12-31T23:59:59.9999999");  //matches DateTime.MinValue and Max
}

function dateTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
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

function tellAxiosError(result, vueObject) {
    toastAxiosResult(result.response, false, vueObject);
}

function tellControllerSuccess(result, vueObject) {
    toastAxiosResult(result, true, vueObject);
}

function toastAxiosResult(controllerResultWithToast, success, vueObject) {
    //Code is meant to support ControllerResult
    var title = success ? "Success" : "Failure";
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastTitle)
        title = controllerResultWithToast.data.toastTitle;

    var text = controllerResultWithToast;
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastText)
        text = controllerResultWithToast.data.toastText + (controllerResultWithToast.data.showStatus ? ("\r\n" + text) : "");

    var variant = success ? 'success' : 'danger';
    var autoHideDelay = success ? 3000 : 10000;

    toast(text, title, variant, autoHideDelay, vueObject);
}

function toast(mesg, title, variant, autoHideDelay, vueObject) {
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
        toast(text, toastTitleOnSuccess, 'success', 3000, vueObject);
    }, function (err) {
        toast(err, toastTitleOnFailure, 'danger', 10000, vueObject);
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
