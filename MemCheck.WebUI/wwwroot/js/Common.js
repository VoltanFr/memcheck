'use strict';

/* exported emptyGuid */
const emptyGuid = '00000000-0000-0000-0000-000000000000';
const toastShortDuration = 4000;
const toastLongDuration = 10000;

function isValidDateTime(utcFromDotNet) {
    return utcFromDotNet && (utcFromDotNet !== '0001-01-01T00:00:00Z') && (utcFromDotNet !== '9999-12-31T23:59:59.9999999');  // matches DateTime.MinValue and Max
}

function dateIsToday(d) {
    return new Date().toDateString() === d.toDateString();
}

function dateIsTomorrow(d) {
    const tomorrow = new Date(new Date().getTime() + (24 * 60 * 60 * 1000));
    return tomorrow.toDateString() === d.toDateString();
}

function toast(mesg, title, success, duration) {
    const actualMesg = `<strong>${title}</strong><br/>${mesg}`;
    const icon = success ? 'thumb-circle' : 'fire';
    const actualDuration = duration ? duration : (success ? toastShortDuration : toastLongDuration);
    // eslint-disable-next-line new-cap
    globalThis.vant.Toast({ message: actualMesg, type: 'html', icon: icon, iconSize: 30, duration: actualDuration, className: 'toast-mesg', position: 'top', closeOnClick: true });
}

/* exported toastWithoutIcon */
function toastWithoutIcon(mesg, title, duration) {
    const actualMesg = `<strong>${title}</strong><br/>${mesg}`;
    const actualDuration = duration ? duration : toastLongDuration;
    // eslint-disable-next-line new-cap
    globalThis.vant.Toast({ message: actualMesg, type: 'html', duration: actualDuration, className: 'toast-mesg', position: 'top', closeOnClick: true });
}

function toastAxiosResult(description, controllerResultWithToast, success) {
    // Code is meant to support ControllerResult
    let title = success ? 'Success' : 'Failure';
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastTitle)
        title = controllerResultWithToast.data.toastTitle;

    let text = controllerResultWithToast;
    if (controllerResultWithToast && controllerResultWithToast.data && controllerResultWithToast.data.toastText)
        text = controllerResultWithToast.data.toastText + (controllerResultWithToast.data.showStatus ? (`\r\n${text}`) : '');
    if (description)
        text = `${description}\r\n${text}`;

    toast(text, title, success);
}

/* exported dateTime */
function dateTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
        return '!';
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (dateIsTomorrow(d))
        return d.toLocaleString([], { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString();
}

/* exported dateTimeWithTime */
function dateTimeWithTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
        return '!';
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleString([], { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

/* exported tellAxiosError */
function tellAxiosError(result, description) {
    if (result.response)
        toastAxiosResult(description, result.response, false);
    else
        toastAxiosResult(description, result, false);
}

/* exported tellControllerSuccess */
function tellControllerSuccess(result) {
    toastAxiosResult('', result, true); // No additional description provided, since the message is meant to have been built by the back-end
}

/* exported base64FromBytes */
function base64FromBytes(bytes) {
    let xml = '';
    const uints = new Uint8Array(bytes);
    for (let j = 0; j < uints.byteLength; j++)
        xml += String.fromCharCode(uints[j]);
    return `data:image/jpg;base64,${window.btoa(xml)}`;
}

/* exported sortTagArray */
function sortTagArray(array) {
    array.sort((tagA, tagB) => (tagA.tagName > tagB.tagName) ? 1 : ((tagB.tagName > tagA.tagName) ? -1 : 0));
}

/* exported sleep */
function sleep(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
}

/* exported copyToClipboardAndToast */
function copyToClipboardAndToast(text, toastTitleOnSuccess, toastTitleOnFailure) {
    navigator.clipboard
        .writeText(text)
        .then(() => {
            toast(text, toastTitleOnSuccess, true);
        })
        .catch(err => {
            toast(err, toastTitleOnFailure, false);
        });
}

/* exported pachAxios */
async function pachAxios(url, timeout) {
    const cancellationTokenSource = axios.CancelToken.source();

    const timeOutId = setTimeout(async() => {
        clearTimeout(timeOutId);
        cancellationTokenSource.cancel(`Timeout of ${timeout} ms (through cancellation token).`);
    }, timeout);

    return await axios.patch(url, {}, { cancelToken: cancellationTokenSource.token })
        .finally(() => {
            clearTimeout(timeOutId);
        });
}

/* exported getCookie */
function getCookie(cookieName) {
    const allCookies = document.cookie;

    if (allCookies.length > 0) {
        const cookieStartIndex = allCookies.indexOf(`${cookieName}=`);

        if (cookieStartIndex !== -1) {
            const valueStartIndex = cookieStartIndex + cookieName.length + 1;
            let valueEndIndex = allCookies.indexOf(';', cookieStartIndex);
            if (valueEndIndex === -1) {
                valueEndIndex = allCookies.length;
            }
            return unescape(allCookies.substring(valueStartIndex, valueEndIndex));
        }
    }
    return '';
}

function setCookie(_, value, days) {
    const maxAge = days * 24 * 60 * 60;
    document.cookie = `name=${value};max-age=${maxAge};path=/`;
}

/* exported deleteCookie */
function deleteCookie(name) {
    setCookie(name, '', -1);
}
