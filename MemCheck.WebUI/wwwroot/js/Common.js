﻿export const emptyGuid = '00000000-0000-0000-0000-000000000000';

export const imageSizeSmall = 1;
export const imageSizeMedium = 2;
export const imageSizeBig = 3;

export const toastShortDuration = 4000;
const toastLongDuration = 10000;

export function isValidDateTime(utcFromDotNet) {
    return utcFromDotNet && (utcFromDotNet !== '0001-01-01T00:00:00Z') && (utcFromDotNet !== '0001-01-01T00:00:00') && (utcFromDotNet !== '9999-12-31T23:59:59.9999999');  // matches DateTime.MinValue and Max
}

function dateIsToday(d) {
    return new Date().toDateString() === d.toDateString();
}

function dateIsTomorrow(d) {
    const tomorrow = new Date(new Date().getTime() + (24 * 60 * 60 * 1000));
    return tomorrow.toDateString() === d.toDateString();
}

export function toast(mesg, title, success, duration) {
    const actualMesg = `<strong>${title}</strong><br/>${mesg}`;
    const icon = success ? 'thumb-circle' : 'fire';
    const actualDuration = duration ? duration : (success ? toastShortDuration : toastLongDuration);
    // eslint-disable-next-line new-cap
    globalThis.vant.Toast({ message: actualMesg, type: 'html', icon: icon, iconSize: 30, duration: actualDuration, className: 'toast-mesg', position: 'top', closeOnClick: true });
}

export function toastWithoutIcon(mesg, title, duration) {
    const actualMesg = `<strong>${title}</strong><br/>${mesg}`;
    const actualDuration = duration ? duration : toastLongDuration;
    // eslint-disable-next-line new-cap
    globalThis.vant.Toast({ message: actualMesg, type: 'html', duration: actualDuration, className: 'toast-mesg', position: 'top', closeOnClick: true });
}

function toastMemCheckControllerResult(axiosResultResponse, success) {
    const title = axiosResultResponse.data.toastTitle;
    const text = axiosResultResponse.data.toastText + (axiosResultResponse.data.showStatus ? (`\r\nStatus: ${axiosResultResponse.status}`) : '');
    toast(text, title, success);
}

function toastAxiosResult(description, axiosResult, success) {
    // controllerResultWithToast can be a ControllerResult or the object returned by Axios

    const title = success ? 'Success' : 'Failure';

    let text;
    if (axiosResult) {
        if (axiosResult.message)
            text = axiosResult.message;
        else {
            if (axiosResult.status)
                text = `Status: ${axiosResult.status}`;
            else
                text = axiosResult;
        }

        if (description)
            text = `${description}\r\n${text}`;
    }
    else
        text = description;

    toast(text, title, success);
}

export function dateTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
        return '!';
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (dateIsTomorrow(d))
        return d.toLocaleString([], { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString();
}

export function dateTimeWithTime(utcFromDotNet) {
    if (!isValidDateTime(utcFromDotNet))
        return '!';
    const d = new Date(utcFromDotNet);
    if (dateIsToday(d))
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleString([], { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export function tellAxiosError(result, description) {
    if (result && result.response && result.response.data && result.response.data.toastTitle) // This is a MemCheck ControllerResult
        toastMemCheckControllerResult(result.response, false);
    else
        toastAxiosResult(description, result, false);
}

export function tellControllerSuccess(result) {
    if (result && result.response && result.response.data && result.response.data.toastTitle) // This is a MemCheck ControllerResult
        toastMemCheckControllerResult(result.response, true);
    else {
        if (result && result.data && result.data.toastTitle) // This is a MemCheck ControllerResult
            toastMemCheckControllerResult(result, true);
        else
            toastAxiosResult('', result, true);
    }
}

export function base64FromBytes(bytes) {
    let xml = '';
    const uints = new Uint8Array(bytes);
    for (let j = 0; j < uints.byteLength; j++)
        xml += String.fromCharCode(uints[j]);
    return `data:image/jpg;base64,${window.btoa(xml)}`;
}

export function sortTagArray(array) {
    array.sort((tagA, tagB) => (tagA.tagName > tagB.tagName) ? 1 : ((tagB.tagName > tagA.tagName) ? -1 : 0));
}

export function sleep(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
}

export function copyToClipboardAndToast(text, toastTitleOnSuccess, toastTitleOnFailure) {
    navigator.clipboard
        .writeText(text)
        .then(() => {
            toast(text, toastTitleOnSuccess, true);
        })
        .catch(err => {
            toast(err, toastTitleOnFailure, false);
        });
}

export async function pachAxios(url, timeout) {
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

export function getCookie(cookieName) {
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

export function setCookie(name, value, days) {
    const maxAge = days * 24 * 60 * 60;
    document.cookie = `${name}=${value};max-age=${maxAge};path=/`;
}

export function deleteCookie(name) {
    setCookie(name, '', -1);
}
