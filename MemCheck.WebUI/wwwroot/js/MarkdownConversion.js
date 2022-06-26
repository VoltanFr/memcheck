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

function insertThousandSeparatorsWhenStartOfInput(wholeMatch, number) {
    const value = valueWithThousandSeparators(number);
    if (value === null)
        return wholeMatch;
    return value;
}

function insertThousandSeparatorsWhenBlankBefore(wholeMatch, blank, number) {
    const value = valueWithThousandSeparators(number);
    if (value === null)
        return wholeMatch;
    return blank + value;
}

function replaceSpaceWithNbsp(wholematch, _spaceAndPunctuation, _space, punctuation, quote, _offset, _wholeInput) {
    if (quote)
        return wholematch;
    return `&nbsp;${punctuation}`;
}

function replaceNumberAndSpaceWithNbsp(_wholeMatch, number, _space, symbol) {
    return `${number}&nbsp;${symbol}`;
}

export function beautifyTextForFrench(src) {
    // This code is not very great: ideally, we should use a real parser to analyze the text and not modify anything in an hyperlink's URL, a backslashed quote, block quotes, and probably embedded HTML.
    // However, I don't have such an implementation now (and I suspect it would be near-impossible, since Markdown is quite ambiguous and not BNF).
    // Fortunately, we don't need to deal with all possible uses of Markdown, but only with MemCheck. So this implementation relies on the presence of a space char, which proves that we are not in an URL.
    // The biggest problem here is we insert thousand separators in years, which is wrong. I don't know yet how to solve that (eg question: "Combien de ml dans un l ?", answer: "1000").

    let result = src;

    // Insert thousand separators when we find a number after a space, or a space and a parenthesis
    result = result.replace(/(\s\(?)(\d+)/g, insertThousandSeparatorsWhenBlankBefore);

    // Insert thousand separators when we find a number at the begining of the text
    result = result.replace(/^(\d+)/g, insertThousandSeparatorsWhenStartOfInput);

    // White space before punctuation becomes nbsp if not in quote
    result = result.replace(/(?<space_and_punctuation>(?<space> )(?<punctuation>\?|!|;|:))|(?<quote>`.+`)/g, replaceSpaceWithNbsp);

    // Digit and white space before unit becomes nbsp
    result = result.replace(/(\d)( )(€|mm|cm|dm|m|km|l|L|hl|bar|h\/km²|°|%)/g, replaceNumberAndSpaceWithNbsp);

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
