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

export function convertMarkdown(src, beautifyForFrench) {
    const actualText = beautifyForFrench ? beautifyTextForFrench(src) : src;
    const converter = new showdown.Converter({ tables: true });
    converter.setOption('openLinksInNewWindow', 'true');
    converter.setOption('simplifiedAutoLink', 'true');
    converter.setOption('simpleLineBreaks', 'true');
    converter.setOption('noHeaderId', 'true');  // For size gain, even if minor
    return converter.makeHtml(actualText);
}
