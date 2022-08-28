import { beautifyTextForFrench, markDownImageCssClassBig } from './MarkdownConversion.js';
import { getMnesiosImageNamesFromSourceText } from './MarkdownConversion.js';
import { replaceMnesiosImagesWithBlobs } from './MarkdownConversion.js';

import { markDownImageCssClassSmall, markDownImageCssClassMedium } from './MarkdownConversion.js';

function expectSetEquals(expected, actual) { // expected is an array, actual is a Set
    expect(actual.size).toBe(expected.length);
    const actualAsArray = [...actual];
    expect(actualAsArray).toEqual(expect.arrayContaining(expected));
}

describe('beautifyTextForFrench: Inputs which must not be beautified', () => {
    test('beautifyTextForFrench: Empty string must not be changed', () => {
        expect(beautifyTextForFrench('')).toBe('');
    });
    test('beautifyTextForFrench: Trivial string must not be changed', () => {
        expect(beautifyTextForFrench('hop')).toBe('hop');
    });
    test('beautifyTextForFrench: Simple Markdown caption must not be changed', () => {
        expect(beautifyTextForFrench('# hop')).toBe('# hop');
    });
});

describe('beautifyTextForFrench: Numeric value at start and unit', () => {
    test('beautifyTextForFrench: 10 € must get nbsp', () => {
        expect(beautifyTextForFrench('10 €')).toBe('10&nbsp;€');
    });
    test('beautifyTextForFrench: 100 € must get nbsp', () => {
        expect(beautifyTextForFrench('100 €')).toBe('100&nbsp;€');
    });
    test('beautifyTextForFrench: 1000 € must get nbsp for € but not for 1000', () => {
        expect(beautifyTextForFrench('1000 €')).toBe('1000&nbsp;€');
    });
    test('beautifyTextForFrench: 10000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('10000 €')).toBe('10&nbsp;000&nbsp;€');
    });
    test('beautifyTextForFrench: 100000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('100000 €')).toBe('100&nbsp;000&nbsp;€');
    });
    test('beautifyTextForFrench: 1000000 € must get three nbsp', () => {
        expect(beautifyTextForFrench('1000000 €')).toBe('1&nbsp;000&nbsp;000&nbsp;€');
    });
    test('beautifyTextForFrench: Multiple numeric values on one line', () => {
        expect(beautifyTextForFrench('100 €, 2000 ml, 30000 cm, 40000 mm, 500000 hl, 6000000 bar')).toBe('100&nbsp;€, 2000&nbsp;ml, 30&nbsp;000&nbsp;cm, 40&nbsp;000&nbsp;mm, 500&nbsp;000&nbsp;hl, 6&nbsp;000&nbsp;000&nbsp;bar');
    });
    test('beautifyTextForFrench: 10 € must get nbsp', () => {
        expect(beautifyTextForFrench('10 €')).toBe('10&nbsp;€');
    });
    test('beautifyTextForFrench: 100 € must get nbsp', () => {
        expect(beautifyTextForFrench('100 €')).toBe('100&nbsp;€');
    });
    test('beautifyTextForFrench: 1000 € must get nbsp for € but not for 1000', () => {
        expect(beautifyTextForFrench('1000 €')).toBe('1000&nbsp;€');
    });
    test('beautifyTextForFrench: 10000 millions must get two nbsp', () => {
        expect(beautifyTextForFrench('10000 millions')).toBe('10&nbsp;000&nbsp;millions');
    });
    test('beautifyTextForFrench: 10000,1234 € must get two nbsp', () => {
        expect(beautifyTextForFrench('10000,1234 €')).toBe('10&nbsp;000,1234&nbsp;€');
    });
    test('beautifyTextForFrench: 100000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('100000 €')).toBe('100&nbsp;000&nbsp;€');
    });
    test('beautifyTextForFrench: 1000000 € must get three nbsp', () => {
        expect(beautifyTextForFrench('1000000 €')).toBe('1&nbsp;000&nbsp;000&nbsp;€');
    });
});

describe('beautifyTextForFrench: Numeric value not at start and unit', () => {
    test('beautifyTextForFrench: 0,001 m³ must get one nbsp', () => {
        expect(beautifyTextForFrench('- 0,001 m³')).toBe('- 0,001&nbsp;m³');
    });
    test('beautifyTextForFrench: 1000000 mm³ must get three nbsp', () => {
        expect(beautifyTextForFrench('- 1000000 mm³')).toBe('- 1&nbsp;000&nbsp;000&nbsp;mm³');
    });
    test('beautifyTextForFrench: Numeric values with coma and units', () => {
        expect(beautifyTextForFrench('Voici donc 10,50 € qui feront en tout 10010,50 €')).toBe('Voici donc 10,50&nbsp;€ qui feront en tout 10&nbsp;010,50&nbsp;€');
    });
    test('beautifyTextForFrench: Numeric values with dot and units', () => {
        expect(beautifyTextForFrench('Voici donc 10.50 € qui feront en tout 10010.50 €')).toBe('Voici donc 10.50&nbsp;€ qui feront en tout 10&nbsp;010.50&nbsp;€');
    });
    test('beautifyTextForFrench: Count of people in parenthesis', () => {
        expect(beautifyTextForFrench('L\'Arkansas a pour capitale et ville la plus peuplée Little Rock (200000 habitants).')).toBe('L\'Arkansas a pour capitale et ville la plus peuplée Little Rock (200&nbsp;000 habitants).');
    });
    test('beautifyTextForFrench: Celsius', () => {
        expect(beautifyTextForFrench('L\'eau bout à 100 °C.')).toBe('L\'eau bout à 100&nbsp;°C.');
    });
    test('beautifyTextForFrench: Percent', () => {
        expect(beautifyTextForFrench('Un taux de chomage de 10 %.')).toBe('Un taux de chomage de 10&nbsp;%.');
    });

    // Not implemented yet:
    // test('beautifyTextForFrench: Negative', () => { expect(beautifyTextForFrench('Soyons -382093890,8909 % positifs')).toBe('Soyons -382&nbsp;093&nbsp;890,8909&nbsp;% positifs'); });
});

describe('beautifyTextForFrench: Numeric value and unit already containing nbsp', () => {
    test('beautifyTextForFrench: 12&nbsp;341 € must get one nbsp', () => {
        expect(beautifyTextForFrench('Hey 12&nbsp;341 € for you')).toBe('Hey 12&nbsp;341&nbsp;€ for you');
    });
    test('beautifyTextForFrench: 120341&nbsp;€ must get one nbsp', () => {
        expect(beautifyTextForFrench(' 120341&nbsp;€ ')).toBe(' 120&nbsp;341&nbsp;€ ');
    });
    test('beautifyTextForFrench: 1&nbsp;250&nbsp;341&nbsp;€ must get no nbsp', () => {
        expect(beautifyTextForFrench('1&nbsp;250&nbsp;341&nbsp;€')).toBe('1&nbsp;250&nbsp;341&nbsp;€');
    });
});

describe('beautifyTextForFrench: Numeric value containing space', () => {
    test('beautifyTextForFrench: 12 341 € must get one nbsp', () => {
        expect(beautifyTextForFrench('7 doh 12 341 € blah 890')).toBe('7 doh 12 341&nbsp;€ blah 890');
    });
});

describe('beautifyTextForFrench: Years', () => {
    test('beautifyTextForFrench: Year at start', () => {
        expect(beautifyTextForFrench('2001 Odyssée')).toBe('2001 Odyssée');
    });
    test('beautifyTextForFrench: Year not at start', () => {
        expect(beautifyTextForFrench('Titre : 2001 Odyssée')).toBe('Titre&nbsp;: 2001 Odyssée');
    });
    test('beautifyTextForFrench: Three years', () => {
        expect(beautifyTextForFrench('2000, 2001, 2002')).toBe('2000, 2001, 2002');
    });
});

describe('beautifyTextForFrench: Punctuation', () => {
    test('beautifyTextForFrench: question must get nbsp', () => {
        expect(beautifyTextForFrench('question ?')).toBe('question&nbsp;?');
    });
    test('beautifyTextForFrench: exclamation must get nbsp', () => {
        expect(beautifyTextForFrench('Exclamons-nous ! Parlons !')).toBe('Exclamons-nous&nbsp;! Parlons&nbsp;!');
    });
});

describe('beautifyTextForFrench: Quotes', () => {
    test('beautifyTextForFrench: empty quote', () => {
        expect(beautifyTextForFrench('``')).toBe('``');
    });
    test('beautifyTextForFrench: two empty quotes', () => {
        expect(beautifyTextForFrench('`` ``')).toBe('`` ``');
    });
    test('beautifyTextForFrench: simple quote', () => {
        expect(beautifyTextForFrench('`hop`')).toBe('`hop`');
    });
    test('beautifyTextForFrench: quote containing only Mnesios image', () => {
        expect(beautifyTextForFrench('`![Mnesios:img]`')).toBe('`![Mnesios:img]`');
    });
    test('beautifyTextForFrench: quote containing text and Mnesios image', () => {
        expect(beautifyTextForFrench('`hop ![Mnesios:img]` jlk')).toBe('`hop ![Mnesios:img]` jlk');
    });
    test('beautifyTextForFrench: question in quote must not be changed', () => {
        expect(beautifyTextForFrench('`a ?`')).toBe('`a ?`');
    });
    test('beautifyTextForFrench: space before semicolon in quote must not be changed', () => {
        expect(beautifyTextForFrench('`a ; b`')).toBe('`a ; b`');
    });
    test('beautifyTextForFrench: Only numeric in quote must not be changed', () => {
        expect(beautifyTextForFrench('`10000`')).toBe('`10000`');
    });
    test('beautifyTextForFrench: Numeric and unit in quote must not be changed - No thousands sep', () => {
        expect(beautifyTextForFrench('`5 €`')).toBe('`5 €`');
    });
    test('beautifyTextForFrench: Numeric and unit in quote must not be changed - Thousands sep', () => {
        expect(beautifyTextForFrench('`54321 km`')).toBe('`54321 km`');
    });
    test('beautifyTextForFrench: Numeric after text in quote must not be changed', () => {
        expect(beautifyTextForFrench('`hop 10000`')).toBe('`hop 10000`');
    });
    test('beautifyTextForFrench: mix of cases including quotes', () => {
        expect(beautifyTextForFrench('10000 € mais `a ; b`\n`10000 ml` 10000 ml')).toBe('10&nbsp;000&nbsp;€ mais `a ; b`\n`10000 ml` 10&nbsp;000&nbsp;ml');
    });
});

describe('beautifyTextForFrench: URLs', () => {
    test('beautifyTextForFrench: URL must not be changed', () => {
        expect(beautifyTextForFrench('https://www.google.com/search?q=%22javascript%22&sxsrf=ALiCzsbRIobu3xxaBH8wJBdwxbHcvE21KQ%3A1654379567179&ei=L9S')).toBe('https://www.google.com/search?q=%22javascript%22&sxsrf=ALiCzsbRIobu3xxaBH8wJBdwxbHcvE21KQ%3A1654379567179&ei=L9S');
    });
});

describe('beautifyTextForFrench: Mixes', () => {
    test('beautifyTextForFrench: Yearn numbers, units', () => {
        expect(beautifyTextForFrench('En 2019, cet État comptait 6 millions d\'habitants, pour une surface d\'environ 180000 km², soit une densité de 34 habitants par km² (la moyenne étant de 33 h/km²).')).toBe('En 2019, cet État comptait 6&nbsp;millions d\'habitants, pour une surface d\'environ 180&nbsp;000&nbsp;km², soit une densité de 34 habitants par km² (la moyenne étant de 33&nbsp;h/km²).');
    });
    test('beautifyTextForFrench: numbers and units', () => {
        expect(beautifyTextForFrench('Son nom vient de sa cylindrée de 12 x 250 cm³ (3 L)')).toBe('Son nom vient de sa cylindrée de 12 x 250&nbsp;cm³ (3&nbsp;L)');
    });
    test('beautifyTextForFrench: numbers and units', () => {
        expect(beautifyTextForFrench('Sportif en plein effort : 100 l/min. Bouteille de 5 l gonflée à 200 bars débitant 15 l/min.')).toBe('Sportif en plein effort&nbsp;: 100&nbsp;l/min. Bouteille de 5&nbsp;l gonflée à 200&nbsp;bars débitant 15&nbsp;l/min.');
    });
});

describe('beautifyTextForFrench: Links with caption and URL, with replacements to make in the caption and various kinds of URL', () => {
    test('beautifyTextForFrench: Nothing to replace, no blanks, no number & unit, no punctuation, no thousands separators', () => {
        expect(beautifyTextForFrench('Lien : [Wikipédia](https://fr.wikipedia.org/)')).toBe('Lien&nbsp;: [Wikipédia](https://fr.wikipedia.org/)');
    });
    test('beautifyTextForFrench: Nothing to replace, blanks, no number & unit, no punctuation, no thousands separators', () => {
        expect(beautifyTextForFrench('[ Le Wikipédia français](https://www.google.com/imgres?imgurl=https%3A%2F%2Fimage.shutterstock.com%2Fimage-photo%2Fnorthern-flicker-snow-storm-260nw-1778937794.jpg&imgrefurl=https%3A%2F%2Fwww.shutterstock.com%2Ffr%2Fsearch%2Fflicker&tbnid=1LrhzjLKTC9KkM&vet=12ahUKEwjCzezrxdP4AhVTVfEDHXmVD0wQMygRegUIARDcAQ..i&docid=-CozTdnUvI2vPM&w=390&h=280&q=flicker%20image&ved=2ahUKEwjCzezrxdP4AhVTVfEDHXmVD0wQMygRegUIARDcAQ)')).toBe('[ Le Wikipédia français](https://www.google.com/imgres?imgurl=https%3A%2F%2Fimage.shutterstock.com%2Fimage-photo%2Fnorthern-flicker-snow-storm-260nw-1778937794.jpg&imgrefurl=https%3A%2F%2Fwww.shutterstock.com%2Ffr%2Fsearch%2Fflicker&tbnid=1LrhzjLKTC9KkM&vet=12ahUKEwjCzezrxdP4AhVTVfEDHXmVD0wQMygRegUIARDcAQ..i&docid=-CozTdnUvI2vPM&w=390&h=280&q=flicker%20image&ved=2ahUKEwjCzezrxdP4AhVTVfEDHXmVD0wQMygRegUIARDcAQ)');
    });
    test('beautifyTextForFrench: Nothing to replace, no blanks, number & unit, no punctuation, no thousands separators', () => {
        expect(beautifyTextForFrench('[50 ml](https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Tal_Flicker.jpg/1130px-Tal_Flicker.jpg) est le lien !')).toBe('[50&nbsp;ml](https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Tal_Flicker.jpg/1130px-Tal_Flicker.jpg) est le lien&nbsp;!');
    });
    test('beautifyTextForFrench: Nothing to replace, no blanks, no number & unit, punctuation, no thousands separators', () => {
        expect(beautifyTextForFrench('99999 fois [LeWikipédiafrançais !](https://camo.githubusercontent.com/e67439e304187b5eafaf9280ff600a8e205c71cbe5392d48ccd40ac590f84408/68747470733a2f2f6769746875622d726561646d652d73746174732e76657263656c2e6170702f6170692f746f702d6c616e67732f3f757365726e616d653d566f6c74616e4672266c61796f75743d636f6d70616374)')).toBe('99&nbsp;999 fois [LeWikipédiafrançais&nbsp;!](https://camo.githubusercontent.com/e67439e304187b5eafaf9280ff600a8e205c71cbe5392d48ccd40ac590f84408/68747470733a2f2f6769746875622d726561646d652d73746174732e76657263656c2e6170702f6170692f746f702d6c616e67732f3f757365726e616d653d566f6c74616e4672266c61796f75743d636f6d70616374)');
    });
    test('beautifyTextForFrench: Nothing to replace, no blanks, no number & unit, no punctuation, thousands separators', () => {
        expect(beautifyTextForFrench('[28078993](https://fr.wikipedia.org/)')).toBe('[28&nbsp;078&nbsp;993](https://fr.wikipedia.org/)');
    });
    test('beautifyTextForFrench: Combination', () => {
        expect(beautifyTextForFrench('[28078993 ! Voici 2 €, et même 78909 € !](https://fr.wikipedia.org/)')).toBe('[28&nbsp;078&nbsp;993&nbsp;! Voici 2&nbsp;€, et même 78&nbsp;909&nbsp;€&nbsp;!](https://fr.wikipedia.org/)');
    });
    test('beautifyTextForFrench: Combination with square brackets', () => {
        expect(beautifyTextForFrench('[28078993 ! Voici 2 €, et même [78909 €] ah !](https://fr.wikipedia.org/)')).toBe('[28&nbsp;078&nbsp;993&nbsp;! Voici 2&nbsp;€, et même [78&nbsp;909&nbsp;€] ah&nbsp;!](https://fr.wikipedia.org/)');
    });
    test('beautifyTextForFrench: Combination with parenthesis', () => {
        expect(beautifyTextForFrench('[28078993 ! Voici 2 €, (et même 78909 €) ah !](https://fr.wikipedia.org/)')).toBe('[28&nbsp;078&nbsp;993&nbsp;! Voici 2&nbsp;€, (et même 78&nbsp;909&nbsp;€) ah&nbsp;!](https://fr.wikipedia.org/)');
    });
    test('beautifyTextForFrench: Combination with square brackets andparenthesis', () => {
        expect(beautifyTextForFrench('avant [28078993 ! Voici 2 €, [doh] (et même 78909 €) ah !](https://fr.wikipedia.org/) après')).toBe('avant [28&nbsp;078&nbsp;993&nbsp;! Voici 2&nbsp;€, [doh] (et même 78&nbsp;909&nbsp;€) ah&nbsp;!](https://fr.wikipedia.org/) après');
    });
});

describe('getMnesiosImageNamesFromSourceText: Inputs without image', () => {
    test('getMnesiosImageNamesFromSourceText: Empty input', () => {
        expect(getMnesiosImageNamesFromSourceText('').size).toBe;
    });
    test('getMnesiosImageNamesFromSourceText: Empty input', () => {
        expect(getMnesiosImageNamesFromSourceText('').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: input with punctuation but no image', () => {
        expect(getMnesiosImageNamesFromSourceText('Exclamons-nous ! Parlons !').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: complex input without image', () => {
        expect(getMnesiosImageNamesFromSourceText('avant [28078993 ! Voici 2 €, [doh] (et même 78909 €) ah !](https://fr.wikipedia.org/) après').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: input with numbers and units', () => {
        expect(getMnesiosImageNamesFromSourceText('Sportif en plein effort : 100 l/min. Bouteille de 5 l gonflée à 200 bars débitant 15 l/min.').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: input with only flat URL', () => {
        expect(getMnesiosImageNamesFromSourceText('https://www.google.com/search?q=%22javascript%22&sxsrf=ALiCzsbRIobu3xxaBH8wJBdwxbHcvE21KQ%3A1654379567179&ei=L9S').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: empty quote', () => {
        expect(getMnesiosImageNamesFromSourceText('``').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: simple quote', () => {
        expect(getMnesiosImageNamesFromSourceText('`hop`').size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: input with quotes but no Mnesios image', () => {
        expect(getMnesiosImageNamesFromSourceText('10000 € mais `a ; b`\n`10000 ml` 10000 ml').size).toBe(0);
    });
});

describe('getMnesiosImageNamesFromSourceText: input is only one image', () => {
    test('getMnesiosImageNamesFromSourceText: small image', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:Situation78979Alabama,size=small]');
        expectSetEquals(['Situation78979Alabama'], result);
    });
    test('getMnesiosImageNamesFromSourceText: medium image', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:AnImageName_7978798,size=medium]');
        expectSetEquals(['AnImageName_7978798'], result);
    });
    test('getMnesiosImageNamesFromSourceText: big image', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:An-Image-Name-797880,size=big]');
        expectSetEquals(['An-Image-Name-797880'], result);
    });
    test('getMnesiosImageNamesFromSourceText: default size', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:78900!An-Image-Name_.();@&=+$/%#]');
        expectSetEquals(['78900!An-Image-Name_.();@&=+$/%#'], result);
    });
    test('getMnesiosImageNamesFromSourceText: special chars', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:An_Image-Name-79788.0(doh);789@MemCheck&Hop=678687+8980uoi$-78900/%#Auio,size=big]');
        expectSetEquals(['An_Image-Name-79788.0(doh);789@MemCheck&Hop=678687+8980uoi$-78900/%#Auio'], result);
    });
    test('getMnesiosImageNamesFromSourceText: with text before and CR after', () => {
        const result = getMnesiosImageNamesFromSourceText('Hey![Mnesios:78900!An-Image-Name_.();@&=+$/%#]\n');
        expectSetEquals(['78900!An-Image-Name_.();@&=+$/%#'], result);
    });
    test('getMnesiosImageNamesFromSourceText: with two blank lines before', () => {
        const imgName = 'An_Image-Name-79788.0(doh);789@Mnesios-tto&Hop=678687+8980uoi$-78900/%#Auio';
        const result = getMnesiosImageNamesFromSourceText(`Hop\n\n![Mnesios:${imgName},size=big]`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains dots', () => {
        const result = getMnesiosImageNamesFromSourceText('Birmingham\n\n![Mnesios:Carte.Amerique.USA.Etats]');
        expectSetEquals(['Carte.Amerique.USA.Etats'], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains accent é', () => {
        const result = getMnesiosImageNamesFromSourceText('Birmingham\n\n![Mnesios:Carte.Amérique.USA.États]');
        expectSetEquals(['Carte.Amérique.USA.États'], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains accent ô', () => {
        const imgName = 'Môle';
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName}]`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains accent Ô', () => {
        const imgName = 'Môle et musoir';
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName}]`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains single quote', () => {
        const imgName = "Duc-d'Albe";
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName}]`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: image name contains space', () => {
        const imgName = 'An image';
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName}]`);
        expectSetEquals([imgName], result);
    });
});

describe('getMnesiosImageNamesFromSourceText: input contains multiple images', () => {
    test('getMnesiosImageNamesFromSourceText: simple', () => {
        const result = getMnesiosImageNamesFromSourceText('![Mnesios:Situation78979Alabama,size=small]![Mnesios:78900!An-Image-Name_.();@&=+$/%#]\n');
        expectSetEquals(['Situation78979Alabama', '78900!An-Image-Name_.();@&=+$/%#'], result);
    });
    test('getMnesiosImageNamesFromSourceText: twice the same image', () => {
        const imgName = 'Situation78979Alabama';
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName},size=small]![Mnesios:${imgName}]\n`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: twice the same image with special chars', () => {
        const imgName = 'A - name with $strange$ things! ; this is @fun (or_not) &3=/%#é.';
        const result = getMnesiosImageNamesFromSourceText(`![Mnesios:${imgName},size=small]![Mnesios:${imgName}]\n`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: multiline', () => {
        const imgNameSmall = 'Situation78979Alabama';
        const imgNameDefaultSize1 = '78900!An-Image-Name_.();@&=+$/%#';
        const imgNameDefaultSize2 = 'An_Image-Name-79788.0(doh);789@Mnesios-tto&Hop=678687+8980uoi$-78900/%#Auio';
        const imgNameBigSize = 'img3 Name';
        const result = getMnesiosImageNamesFromSourceText(`Welcome to Mnesios, a [Fun tool](https://mnesios.com/)\nSee that image: ![Mnesios:${imgNameSmall},size=small]\nOr that: ![Mnesios:${imgNameDefaultSize1}]\n\n![Mnesios:${imgNameBigSize},size=big]![Mnesios:${imgNameDefaultSize2}] ![Mnesios:${imgNameSmall},size=big]`);
        expectSetEquals([imgNameSmall, imgNameDefaultSize1, imgNameDefaultSize2, imgNameBigSize], result);
    });
});

describe('getMnesiosImageNamesFromSourceText: images and quotes', () => {
    test('getMnesiosImageNamesFromSourceText: whole input is a quote without image', () => {
        const result = getMnesiosImageNamesFromSourceText('`quote`');
        expect(result.size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: whole input is an image in a quote', () => {
        const result = getMnesiosImageNamesFromSourceText('`![Mnesios:an img]`');
        expect(result.size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: whole input is an image and text in a quote', () => {
        const result = getMnesiosImageNamesFromSourceText('`image non affichée : ![Mnesios:img]`');
        expect(result.size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: two quotes', () => {
        const result = getMnesiosImageNamesFromSourceText('`image non affichée : ![Mnesios:img]` and `Pas ![Mnesios:hop9 879879] affichée`');
        expect(result.size).toBe(0);
    });
    test('getMnesiosImageNamesFromSourceText: a quote and an image', () => {
        const imgName = 'I-m g';
        const result = getMnesiosImageNamesFromSourceText(`\`image non affichée : ![Mnesios:${imgName}]\` image affichée : ![Mnesios:${imgName},size=small]`);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: an image and a quote', () => {
        const imgName = 'I!mg';
        const result = getMnesiosImageNamesFromSourceText(`image affichée : ![Mnesios:${imgName},size=big] \`image non affichée : ![Mnesios:${imgName}]\``);
        expectSetEquals([imgName], result);
    });
    test('getMnesiosImageNamesFromSourceText: complex', () => {
        const bigImg0Name = 'big';
        const mediumImg0Name = 'medium=0';
        const mediumImg1Name = 'medium@1';
        const smallImgName = '(small';
        const noSizeImgName = '!noSize';
        const result = getMnesiosImageNamesFromSourceText(`text ![Mnesios:${mediumImg0Name},size=medium] \`![Mnesios:toto]\` ![Mnesios:${bigImg0Name},size=big] \`\` ![Mnesios:${mediumImg1Name},size=medium] \`quote\` ![Mnesios:${bigImg0Name},size=big] ![Mnesios:${noSizeImgName}] ![Mnesios:${smallImgName},size=small] \`![Mnesios:hop,size=big]\``);
        expectSetEquals([bigImg0Name, mediumImg0Name, mediumImg1Name, smallImgName, noSizeImgName], result);
    });
});

describe('replaceMnesiosImagesWithBlobs: cases with no replacement', () => {
    test('replaceMnesiosImagesWithBlobs: empty input', () => {
        const mnesiosImageDefinitions = [];
        const result = replaceMnesiosImagesWithBlobs('', mnesiosImageDefinitions);
        expect(result).toBe('');
    });
    test('replaceMnesiosImagesWithBlobs: whole input is a quote without image', () => {
        const mnesiosImageDefinitions = [];
        const result = replaceMnesiosImagesWithBlobs('`hop`', mnesiosImageDefinitions);
        expect(result).toBe('`hop`');
    });
    test('replaceMnesiosImagesWithBlobs: whole input is a quote with an image', () => {
        const imageName = '!my img;';
        const mnesiosImageDefinitions = [{ name: imageName, blob: 'A' }];
        const result = replaceMnesiosImagesWithBlobs(`\`![Mnesios:${imageName}]\``, mnesiosImageDefinitions);
        expect(result).toBe(`\`![Mnesios:${imageName}]\``);
    });
    test('replaceMnesiosImagesWithBlobs: complex', () => {
        const imageName = '@img ;';
        const mnesiosImageDefinitions = [{ name: imageName, blob: 'A' }];
        const input = `Hop ! [Un lien](https://keep.google.com/u/0/#home) \`![Mnesios:${imageName}]\` https://keep.google.com/`;
        const result = replaceMnesiosImagesWithBlobs(input, mnesiosImageDefinitions);
        expect(result).toBe(input);
    });
});

describe('replaceMnesiosImagesWithBlobs: cases with replacement', () => {
    test('replaceMnesiosImagesWithBlobs: whole input is an image with no size specified', () => {
        const name = '(my) img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name, blob }];
        const sourceText = `![Mnesios:${name}]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': name, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${name}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: whole input is an image with small size', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=small]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassSmall}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: whole input is an image with medium size', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=medium]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: whole input is an image with accent in name', () => {
        const imageName = 'Môle et musoir';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=medium]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: whole input is an image with big size', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=big]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: input is an image with big size with text before', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `XYZ![Mnesios:${imageName},size=big]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`XYZ<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: input is an image with medium size with text after', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=big]GHJ`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>GHJ`);
    });
    test('replaceMnesiosImagesWithBlobs: input is an image with medium size with text before and after', () => {
        const imageName = 'img';
        const blob = 'A';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `880980![Mnesios:${imageName},size=big]30 cm`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`880980<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>30 cm`);
    });
    test('replaceMnesiosImagesWithBlobs: input contains two different images and text', () => {
        const image1Name = 'img1';
        const blob1 = 'Blob1';
        const image2Name = 'img2.État.bé';
        const blob2 = 'Blob2';
        const mnesiosImageDefinitions = [{ name: image1Name, blob: blob1 }, { name: image2Name, blob: blob2 }];
        const sourceText = `G![Mnesios:${image1Name}] L\n ![Mnesios:${image2Name},size=big]P`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const image1Base64 = btoa(JSON.stringify({ 'name': image1Name, 'blob': blob1 }));
        const image2Base64 = btoa(JSON.stringify({ 'name': image2Name, 'blob': blob2 }));
        expect(result).toBe(`G<div class='markdown-render-image-div'><img src='${blob1}' alt='${image1Name}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${image1Base64}");'/></div> L\n <div class='markdown-render-image-div'><img src='${blob2}' alt='${image2Name}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${image2Base64}");'/></div>P`);
    });
    test('replaceMnesiosImagesWithBlobs: input contains only twice the same image with same size', () => {
        const imageName = 'img';
        const blob = 'Blob';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `![Mnesios:${imageName},size=big] ![Mnesios:${imageName},size=big]`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div> <div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: input contains four times the same image with various sizes and text', () => {
        const imageName = 'img';
        const blob = 'Blob';
        const mnesiosImageDefinitions = [{ name: imageName, blob: blob }];
        const sourceText = `DEFAULT![Mnesios:${imageName}]BIG![Mnesios:${imageName},size=big]SMALL![Mnesios:${imageName},size=small]MEDIUM![Mnesios:${imageName},size=medium]END`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const imageBase64 = btoa(JSON.stringify({ 'name': imageName, 'blob': blob }));
        const expected = `DEFAULT<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`
            + `BIG<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`
            + `SMALL<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassSmall}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`
            + `MEDIUM<div class='markdown-render-image-div'><img src='${blob}' alt='${imageName}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${imageBase64}");'/></div>`
            + 'END';
        expect(result).toBe(expected);
    });
    test('replaceMnesiosImagesWithBlobs: complex case', () => {
        const image1Name = 'img #1';
        const blob1 = 'Blob1';
        const image2Name = 'i.é-m!g2';
        const blob2 = 'a ,';
        const image3Name = 'i 3';
        const blob3 = '@b3';
        const mnesiosImageDefinitions = [{ name: image1Name, blob: blob1 }, { name: image2Name, blob: blob2 }, { name: image3Name, blob: blob3 }];
        const sourceText = `\`QUOTE\`![Mnesios:${image1Name}]\`![Mnesios:${image2Name},size=big]\`![Mnesios:${image2Name},size=big]SMALL![Mnesios:${image3Name},size=small]MEDIUM![Mnesios:${image2Name},size=medium]END`;
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, 'some_code;');
        const image1Base64 = btoa(JSON.stringify({ 'name': image1Name, 'blob': blob1 }));
        const image2Base64 = btoa(JSON.stringify({ 'name': image2Name, 'blob': blob2 }));
        const image3Base64 = btoa(JSON.stringify({ 'name': image3Name, 'blob': blob3 }));
        const expected = `\`QUOTE\`<div class='markdown-render-image-div'><img src='${blob1}' alt='${image1Name}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${image1Base64}");'/></div>`
            + `\`![Mnesios:${image2Name},size=big]\``
            + `<div class='markdown-render-image-div'><img src='${blob2}' alt='${image2Name}' class='${markDownImageCssClassBig}' onclick='some_code; imageClicked("${image2Base64}");'/></div>`
            + `SMALL<div class='markdown-render-image-div'><img src='${blob3}' alt='${image3Name}' class='${markDownImageCssClassSmall}' onclick='some_code; imageClicked("${image3Base64}");'/></div>`
            + `MEDIUM<div class='markdown-render-image-div'><img src='${blob2}' alt='${image2Name}' class='${markDownImageCssClassMedium}' onclick='some_code; imageClicked("${image2Base64}");'/></div>`
            + 'END';
        expect(result).toBe(expected);
    });
    test('replaceMnesiosImagesWithBlobs: contains a quote in name', () => {
        const name = "quote '";
        const blob = 'blob';
        const image = { name, blob };
        const mnesiosImageDefinitions = [image];
        const sourceText = `![Mnesios:${name}]`;
        const onClickMethod = "const div = document.querySelector('#AuthoringMainDiv'); const thisApp=div.__vue_app__; const imageClicked=thisApp._component.methods.showImageFull;";
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, onClickMethod);
        const imageStringified = `{"name":"${name}","blob":"${blob}"}`;
        const base64 = btoa(imageStringified);
        const expectedName = 'quote &#39;';
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${expectedName}' class='${markDownImageCssClassMedium}' onclick='${onClickMethod} imageClicked("${base64}");'/></div>`);
    });
    test('replaceMnesiosImagesWithBlobs: contains two quotes in name', () => {
        const name = " 'xxx' ";
        const blob = 'blob';
        const image = { name, blob };
        const mnesiosImageDefinitions = [image];
        const sourceText = `![Mnesios:${name}]`;
        const onClickMethod = "const div = document.querySelector('#AuthoringMainDiv'); const thisApp=div.__vue_app__; const imageClicked=thisApp._component.methods.showImageFull;";
        const result = replaceMnesiosImagesWithBlobs(sourceText, mnesiosImageDefinitions, onClickMethod);
        const imageStringified = `{"name":"${name}","blob":"${blob}"}`;
        const base64 = btoa(imageStringified);
        const expectedName = ' &#39;xxx&#39; ';
        expect(result).toBe(`<div class='markdown-render-image-div'><img src='${blob}' alt='${expectedName}' class='${markDownImageCssClassMedium}' onclick='${onClickMethod} imageClicked("${base64}");'/></div>`);
    });
});
