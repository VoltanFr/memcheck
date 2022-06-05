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

function valueWithThousandSeparators(number) { // number is a string
    const value = Number(number);
    if (isNaN(value)) // We could decide not to convert a number < 2100 since we can suspect it is a year
        return null;
    let result = value.toLocaleString('fr-FR');
    result = result.replace(' ', '&nbsp;');
    return result;
}

function insertThousandSeparatorsWhenStartOfInput(wholeMatch, number) {
    const value = valueWithThousandSeparators(number);
    if (value === null || value === 0)
        return wholeMatch;
    return value;
}

function insertThousandSeparatorsWhenBlankBefore(wholeMatch, blank, number) {
    const value = valueWithThousandSeparators(number);
    if (value === null || value === 0)
        return wholeMatch;
    return blank + value;
}

function replaceSpaceWithNbsp(_wholeMatch, _space, symbol) {
    return `&nbsp;${symbol}`;
}

function replaceNumberAndSpaceWithNbsp(_wholeMatch, number, _space, symbol) {
    return `${number}&nbsp;${symbol}`;
}

function beautifyTextForFrench(src) {
    // This code is not very great: ideally, we should use a real parser to analyze the text and not modify anything in an hyperlink's URL, a backslashed quote, block quotes, and probably embedded HTML.
    // However, I don't have such an implementation now (and I suspect it would be near-impossible, since Markdown is quite ambiguous and not BNF).
    // Fortunately, we don't need to deal with all possible uses of Markdown, but only with MemCheck. So this implementation relies on the presence of a space char, which proves that we are not in an URL.
    // The biggest problem here is we insert thousand separators in years, which is wrong. I don't know yet how to solve that (eg question: "Combien de ml dans un l ?", answer: "1000").

    let result = src;

    // Insert thousand separators
    result = result.replace(/(\s)(\d+)/g, insertThousandSeparatorsWhenBlankBefore);
    result = result.replace(/^(\d+)/g, insertThousandSeparatorsWhenStartOfInput);

    // White space before punctuation becomes nbsp
    result = result.replace(/( )(\?|!|;|:)/g, replaceSpaceWithNbsp);

    // Digit and white space before unit becomes nbsp
    result = result.replace(/(\d)( )(€|mm|cm|dm|m|km|l|L|hl|bar|h\/km²)/g, replaceNumberAndSpaceWithNbsp);

    return result;
}

/* exported convertMarkdown */
function convertMarkdown(src, beautifyForFrench) {
    const acutalText = beautifyForFrench ? beautifyTextForFrench(src) : src;
    const converter = new showdown.Converter({ tables: true });
    converter.setOption('openLinksInNewWindow', 'true');
    converter.setOption('simplifiedAutoLink', 'true');
    converter.setOption('simpleLineBreaks', 'true');
    converter.setOption('noHeaderId', 'true');  // For size gain, even if minor
    return converter.makeHtml(acutalText);
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
