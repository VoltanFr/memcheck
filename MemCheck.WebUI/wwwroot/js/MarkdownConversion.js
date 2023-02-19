import { imageSizeSmall } from './Common.js';
import { imageSizeMedium } from './Common.js';
import { imageSizeBig } from './Common.js';

const charsAllowedInImageName = '[-\' _.();!@&=+$/%#a-zA-Z0-9\u00C0-\u017F]+'; // The last range is for accents. This information is duplicated in the C# class QueryValidationHelper
const imageDivCssClass = 'markdown-render-image-div';

export const markDownImageCssClassSmall = 'markdown-render-image-small';
export const markDownImageCssClassMedium = 'markdown-render-image-medium';
export const markDownImageCssClassBig = 'markdown-render-image-big';

function valueWithThousandSeparators(number) { // number is a string
    const value = Number(number);
    if (isNaN(value))
        return null;
    if (value === 0) // We don't want to transform '000' to '0', for example
        return null;
    if (value > -6000 && value < 2200) // This might be a year, which doesn't take a thousand separator
        return null;
    let result = value.toLocaleString('fr-FR');
    result = result.replace(/\s/g, '&nbsp;');
    return result;
}

function insertThousandSeparatorsWhenStartOfInput(wholeMatch, openingSquareBracket, number, _offset, _wholeInput) {
    const value = valueWithThousandSeparators(number);
    if (value === null)
        return wholeMatch;
    if (openingSquareBracket)
        return `${openingSquareBracket}${value}`;
    return value;
}

function insertThousandSeparatorsWhenBlankBefore(wholeMatch, separator, number, quote, _offset, _wholeInput) {
    if (quote)
        return wholeMatch;
    const value = valueWithThousandSeparators(number);
    if (value === null)
        return wholeMatch;
    return separator + value;
}

function replaceSpaceWithNbsp(wholeMatch, _spaceAndPunctuation, _space, punctuation, quote, _offset, _wholeInput) {
    if (quote)
        return wholeMatch;
    return `&nbsp;${punctuation}`;
}

function replaceNumberAndSpaceWithNbsp(wholeMatch, digit, _space, symbol, quote, _offset, _wholeInput) {
    if (quote)
        return wholeMatch;
    return `${digit}&nbsp;${symbol}`;
}

export function beautifyTextForFrench(src) {
    // This code is not very great: ideally, we should use a real parser to analyze the text
    // However, I don't have such an implementation now (and I suspect it would be near-impossible, since Markdown is quite ambiguous and not BNF).
    // Fortunately, we don't need to deal with all possible uses of Markdown, but only with MemCheck. So this implementation relies on the presence of a space char, which proves that we are not in an URL.

    let result = src;

    // The case of a link with two parts: [link_caption](url). In that case, the url is not to be modified, and the caption is to be treated as all other text
    result = result.replace(/(?<space_and_optional_open_parenth> \(?)(?<number>\d+)|(?<quote>`.+`)/g, insertThousandSeparatorsWhenBlankBefore);

    // Insert thousand separators when we find a number after an opening bracket, a space, a space and a parenthesis, or an opening bracket and a parenthesis
    result = result.replace(/(?<separator> |\[\(?)(?<number>\d+)|(?<quote>`.+`)/g, insertThousandSeparatorsWhenBlankBefore);

    // Insert thousand separators when we find a number at the begining of the text, with an optional opening square bracket before
    result = result.replace(/^(?<opening_sq_brcket>\[)?(?<number>\d+)/g, insertThousandSeparatorsWhenStartOfInput);

    // White space before punctuation becomes nbsp if not in quote
    result = result.replace(/(?<space_and_punctuation>(?<space> )(?<punctuation>\?|!|;|:))|(?<quote>`.+`)/g, replaceSpaceWithNbsp);

    // Digit and white space before unit becomes nbsp
    result = result.replace(/(?<symbol>\d)(?<space> )(?<unit>€|mm|cm|dm|m|km|l|L|hl|bar|h\/km²|°|%)|(?<quote>`.+`)/g, replaceNumberAndSpaceWithNbsp);

    return result;
}

function imageNameRegExp(size) {
    let sizePart;
    switch (size) {
        case imageSizeSmall: sizePart = ',size=small'; break;
        case imageSizeMedium: sizePart = ',size=medium'; break;
        case imageSizeBig: sizePart = ',size=big'; break;
        default: sizePart = ''; break;
    }
    const result = new RegExp(`(?<image>!\\[Mnesios:(?<imageName>${charsAllowedInImageName})${sizePart}\\])|(?<quote>\`([^\`]+)?\`)`, 'g');
    // console.log(result.source);
    return result;
}

function getMnesiosImageNamesFromSourceTextForSize(src, imageSize) {
    const arrayed = Array.from(src.matchAll(imageNameRegExp(imageSize)));
    const images = arrayed.filter(match => match.groups.imageName);
    return images.map(match => { return match.groups.imageName; });
}

export function encodeImageDefinition(image) {
    const imageStringified = JSON.stringify(image);
    const result = btoa(unescape(encodeURIComponent(imageStringified))); // The stringified version contains double quotes, and may contain quotes (in the image name or description)
    return result;
}

export function decodeImageDefinition(encodedImage) {
    const decoded = decodeURIComponent(escape(atob(encodedImage)));
    const result = JSON.parse(decoded);
    return result;
}

export function getMnesiosImageNamesFromSourceText(src) {
    // Image format in text: ![Mnesios:Image-name-without-space-or-comma,width=small|medium|big]
    // result is a Set of image names (string)

    let result = new Set();
    getMnesiosImageNamesFromSourceTextForSize(src, imageSizeSmall).forEach(item => result.add(item));
    getMnesiosImageNamesFromSourceTextForSize(src, imageSizeMedium).forEach(item => result.add(item));
    getMnesiosImageNamesFromSourceTextForSize(src, imageSizeBig).forEach(item => result.add(item));
    getMnesiosImageNamesFromSourceTextForSize(src).forEach(item => result.add(item));
    return result;
}

// I use global variables because I need to access these values in replaceMnesiosImageWithBlob, to which I can not pass additional args. There has to be a better way
let globalMnesiosImageDefinitions = [];
let globalImageOnClickFunction = null;
let globalCssClass = null;

export function tell() {
    alert('told');
}

function replaceMnesiosImageWithBlob(wholeMatch, _image, imageName, _sizePart, quote, _offset, _wholeInput) {
    if (quote)
        return wholeMatch;
    const imageDefinitionFromGlobal = globalMnesiosImageDefinitions.find(imageDefinition => imageDefinition.name === imageName);
    const blob = imageDefinitionFromGlobal ? (imageDefinitionFromGlobal.blob ? imageDefinitionFromGlobal.blob : '') : 'image unknown';
    const base64 = encodeImageDefinition(imageDefinitionFromGlobal);
    const alt = imageName.replace(/[']/g, '&#39;'); // Quotes would close the attribute value
    return `<div class='${imageDivCssClass}'><img src='${blob}' alt='${alt}' class='${globalCssClass}' onclick='${globalImageOnClickFunction} imageClicked("${base64}");` + '\'/></div>';
}

export function replaceMnesiosImagesWithBlobs(src, mnesiosImageDefinitions, methodToCallOnClick) {
    // mnesiosImageDefinitions is an array of {name: image name, blob: the blob to use a image definition}
    globalMnesiosImageDefinitions = mnesiosImageDefinitions;
    globalImageOnClickFunction = methodToCallOnClick;
    globalCssClass = markDownImageCssClassMedium;
    let result = src.replace(imageNameRegExp(), replaceMnesiosImageWithBlob);
    result = result.replace(imageNameRegExp(imageSizeMedium), replaceMnesiosImageWithBlob);
    globalCssClass = markDownImageCssClassSmall;
    result = result.replace(imageNameRegExp(imageSizeSmall), replaceMnesiosImageWithBlob);
    globalCssClass = markDownImageCssClassBig;
    result = result.replace(imageNameRegExp(imageSizeBig), replaceMnesiosImageWithBlob);
    return result;
}

export function convertMarkdown(src, beautifyForFrench, mnesiosImageDefinitions, methodToCallOnClick) {
    // mnesiosImageDefinitions is an array of {name: image name, blob: the blob to use a image definition}
    try {
        const textWithMnesiosImageBlobs = mnesiosImageDefinitions ? replaceMnesiosImagesWithBlobs(src, mnesiosImageDefinitions, methodToCallOnClick) : src;
        const beautifiedText = beautifyForFrench ? beautifyTextForFrench(textWithMnesiosImageBlobs) : textWithMnesiosImageBlobs;
        const converter = new showdown.Converter({ tables: true });
        converter.setOption('openLinksInNewWindow', 'true');
        converter.setOption('simplifiedAutoLink', 'true');
        converter.setOption('simpleLineBreaks', 'true');
        converter.setOption('noHeaderId', 'true');  // For size gain, even if minor
        return converter.makeHtml(beautifiedText);
    }
    catch (error) {
        return src;
    }
}
