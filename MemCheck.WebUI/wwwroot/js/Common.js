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

function dateTimeWithTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
        return "!";
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleString([], { year: "numeric", month: "numeric", day: "numeric", hour: '2-digit', minute: '2-digit' });
}

function dateIsToday(d) {
    return new Date().toDateString() == d.toDateString();
}

function dateIsTomorrow(d) {
    const tomorrow = new Date(new Date().getTime() + (24 * 60 * 60 * 1000));
    return tomorrow.toDateString() == d.toDateString();
}

function tellAxiosError(result) {
    toastAxiosResult(result.response, false);
}

function tellControllerSuccess(result) {
    toastAxiosResult(result, true);
}

function toastAxiosResult(controllerResultWithToast, success) {
    //Code is meant to support ControllerResult
    var title = success ? "Success" : "Failure";
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastTitle)
        title = controllerResultWithToast.data.toastTitle;

    var text = controllerResultWithToast;
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastText)
        text = controllerResultWithToast.data.toastText + (controllerResultWithToast.data.showStatus ? ("\r\n" + text) : "");

    toast(text, title, success);
}

function toast(mesg, title, success) {
    const actualMesg = "<strong>" + title + "</strong><br/>" + mesg;
    const icon = success ? "thumb-circle" : "fire";
    const duration = success ? 4000 : 10000;
    globalThis.vant.Toast({ message: actualMesg, type: "html", icon: icon, iconSize: 30, duration: duration, className: "toast-mesg", position: "top" });
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

function copyToClipboardAndToast(text, toastTitleOnSuccess, toastTitleOnFailure) {
    navigator.clipboard.writeText(text)
        .then(function () {
            toast(text, toastTitleOnSuccess, true);
        }
            , function (err) {
                toast(err, toastTitleOnFailure, false);
            });
}

function beautifyTextForFrench(src) {
    var result = src.replace(/\s\?/g, "&nbsp;?");
    result = result.replace(/\s!/g, "&nbsp;!");
    result = result.replace(/\s;/g, "&nbsp;;");
    result = result.replace(/\s:/g, "&nbsp;:");
    return result;
}

function convertMarkdown(src, beautifyForFrench) {
    const acutalText = beautifyForFrench ? beautifyTextForFrench(src) : src;
    var converter = new showdown.Converter({ tables: true });
    converter.setOption('openLinksInNewWindow', 'true');
    converter.setOption('simplifiedAutoLink', 'true');
    converter.setOption('simpleLineBreaks', 'true');
    return converter.makeHtml(acutalText);
}

async function pachAxios(url, timeout) {
    const cancellationTokenSource = axios.CancelToken.source();

    let timeOutId =
        setTimeout(async () => {
            clearTimeout(timeOutId);
            cancellationTokenSource.cancel(`Timeout of ${timeout} ms (through cancellation token).`);
        }, timeout);

    return await axios.patch(url, {}, { cancelToken: cancellationTokenSource.token })
        .finally(() => {
            clearTimeout(timeOutId);
        });
}

function getCookie(c_name) {
    if (document.cookie.length > 0) {
        c_start = document.cookie.indexOf(c_name + "=");
        if (c_start != -1) {
            c_start = c_start + c_name.length + 1;
            c_end = document.cookie.indexOf(";", c_start);
            if (c_end == -1) {
                c_end = document.cookie.length;
            }
            return unescape(document.cookie.substring(c_start, c_end));
        }
    }
    return "";
}

function setCookie(name, value, days) {
    var expires;
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toGMTString();
    }
    else {
        expires = "";
    }
    document.cookie = name + "=" + value + expires + "; path=/";
}

function deleteCookie(name) {
    setCookie(name, "", -1);
}
