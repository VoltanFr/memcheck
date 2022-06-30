import { beautifyTextForFrench } from './MarkdownConversion.js';

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
