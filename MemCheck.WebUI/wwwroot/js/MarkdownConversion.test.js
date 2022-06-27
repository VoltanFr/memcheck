import { beautifyTextForFrench } from './MarkdownConversion.js';

describe('Inputs which must not be beautified', () => {
    test('Empty string must not be changed', () => {
        expect(beautifyTextForFrench('')).toBe('');
    });
    test('Trivial string must not be changed', () => {
        expect(beautifyTextForFrench('hop')).toBe('hop');
    });
    test('Simple Markdown caption must not be changed', () => {
        expect(beautifyTextForFrench('# hop')).toBe('# hop');
    });
});

describe('Numeric value at start and unit', () => {
    test('10 € must get nbsp', () => {
        expect(beautifyTextForFrench('10 €')).toBe('10&nbsp;€');
    });
    test('100 € must get nbsp', () => {
        expect(beautifyTextForFrench('100 €')).toBe('100&nbsp;€');
    });
    test('1000 € must get nbsp for € but not for 1000', () => {
        expect(beautifyTextForFrench('1000 €')).toBe('1000&nbsp;€');
    });
    test('10000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('10000 €')).toBe('10&nbsp;000&nbsp;€');
    });
    test('100000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('100000 €')).toBe('100&nbsp;000&nbsp;€');
    });
    test('1000000 € must get three nbsp', () => {
        expect(beautifyTextForFrench('1000000 €')).toBe('1&nbsp;000&nbsp;000&nbsp;€');
    });
    test('Multiple numeric values on one line', () => {
        expect(beautifyTextForFrench('100 €, 2000 ml, 30000 cm, 40000 mm, 500000 hl, 6000000 bar')).toBe('100&nbsp;€, 2000&nbsp;ml, 30&nbsp;000&nbsp;cm, 40&nbsp;000&nbsp;mm, 500&nbsp;000&nbsp;hl, 6&nbsp;000&nbsp;000&nbsp;bar');
    });
});

describe('Numeric value not at start and unit', () => {
    test('0,001 m³ must get one nbsp', () => {
        expect(beautifyTextForFrench('- 0,001 m³')).toBe('- 0,001&nbsp;m³');
    });
    test('10 € must get nbsp', () => {
        expect(beautifyTextForFrench('10 €')).toBe('10&nbsp;€');
    });
    test('100 € must get nbsp', () => {
        expect(beautifyTextForFrench('100 €')).toBe('100&nbsp;€');
    });
    test('1000 € must get nbsp for € but not for 1000', () => {
        expect(beautifyTextForFrench('1000 €')).toBe('1000&nbsp;€');
    });
    test('10000 millions must get two nbsp', () => {
        expect(beautifyTextForFrench('10000 millions')).toBe('10&nbsp;000&nbsp;millions');
    });
    test('10000,1234 € must get two nbsp', () => {
        expect(beautifyTextForFrench('10000,1234 €')).toBe('10&nbsp;000,1234&nbsp;€');
    });
    test('100000 € must get two nbsp', () => {
        expect(beautifyTextForFrench('100000 €')).toBe('100&nbsp;000&nbsp;€');
    });
    test('1000000 € must get three nbsp', () => {
        expect(beautifyTextForFrench('1000000 €')).toBe('1&nbsp;000&nbsp;000&nbsp;€');
    });
    test('1000000 mm³ must get three nbsp', () => {
        expect(beautifyTextForFrench('- 1000000 mm³')).toBe('- 1&nbsp;000&nbsp;000&nbsp;mm³');
    });
    test('Numeric values with coma and units', () => {
        expect(beautifyTextForFrench('Voici donc 10,50 € qui feront en tout 10010,50 €')).toBe('Voici donc 10,50&nbsp;€ qui feront en tout 10&nbsp;010,50&nbsp;€');
    });
    test('Numeric values with dot and units', () => {
        expect(beautifyTextForFrench('Voici donc 10.50 € qui feront en tout 10010.50 €')).toBe('Voici donc 10.50&nbsp;€ qui feront en tout 10&nbsp;010.50&nbsp;€');
    });
    test('Count of people in parenthesis', () => {
        expect(beautifyTextForFrench('L\'Arkansas a pour capitale et ville la plus peuplée Little Rock (200000 habitants).')).toBe('L\'Arkansas a pour capitale et ville la plus peuplée Little Rock (200&nbsp;000 habitants).');
    });
    test('Celsius', () => {
        expect(beautifyTextForFrench('L\'eau bout à 100 °C.')).toBe('L\'eau bout à 100&nbsp;°C.');
    });
    test('Percent', () => {
        expect(beautifyTextForFrench('Un taux de chomage de 10 %.')).toBe('Un taux de chomage de 10&nbsp;%.');
    });

    // Not implemented yet:
    // test('Negative', () => { expect(beautifyTextForFrench('Soyons -382093890,8909 % positifs')).toBe('Soyons -382&nbsp;093&nbsp;890,8909&nbsp;% positifs'); });
});

describe('Numeric value and unit already containing nbsp', () => {
    test('12&nbsp;341 € must get one nbsp', () => {
        expect(beautifyTextForFrench('Hey 12&nbsp;341 € for you')).toBe('Hey 12&nbsp;341&nbsp;€ for you');
    });
    test('120341&nbsp;€ must get one nbsp', () => {
        expect(beautifyTextForFrench(' 120341&nbsp;€ ')).toBe(' 120&nbsp;341&nbsp;€ ');
    });
    test('1&nbsp;250&nbsp;341&nbsp;€ must get no nbsp', () => {
        expect(beautifyTextForFrench('1&nbsp;250&nbsp;341&nbsp;€')).toBe('1&nbsp;250&nbsp;341&nbsp;€');
    });
});

describe('Numeric value containing space', () => {
    test('12 341 € must get one nbsp', () => {
        expect(beautifyTextForFrench('7 doh 12 341 € blah 890')).toBe('7 doh 12 341&nbsp;€ blah 890');
    });
});

describe('Years', () => {
    test('Year at start', () => {
        expect(beautifyTextForFrench('2001 Odyssée')).toBe('2001 Odyssée');
    });
    test('Year not at start', () => {
        expect(beautifyTextForFrench('Titre : 2001 Odyssée')).toBe('Titre&nbsp;: 2001 Odyssée');
    });
    test('Three years', () => {
        expect(beautifyTextForFrench('2000, 2001, 2002')).toBe('2000, 2001, 2002');
    });
});

describe('Punctuation', () => {
    test('question must get nbsp', () => {
        expect(beautifyTextForFrench('question ?')).toBe('question&nbsp;?');
    });
    test('exclamation must get nbsp', () => {
        expect(beautifyTextForFrench('Exclamons-nous ! Parlons !')).toBe('Exclamons-nous&nbsp;! Parlons&nbsp;!');
    });
});

describe('Quotes', () => {
    test('question in quote must not be changed', () => {
        expect(beautifyTextForFrench('`a ?`')).toBe('`a ?`');
    });
    test('space before semicolon in quote must not be changed', () => {
        expect(beautifyTextForFrench('`a ; b`')).toBe('`a ; b`');
    });
    test('Only numeric in quote must not be changed', () => {
        expect(beautifyTextForFrench('`10000`')).toBe('`10000`');
    });
    // test('Numeric after text in quote must not be changed', () => {
    //     expect(beautifyTextForFrench('`hop 10000`')).toBe('`hop 10000`');
    // });

    // To be fixed
    // test('mix of cases including quotes', () => { expect(beautifyTextForFrench('10000 € mais `a ; b`\n`10000 ml` 10000 ml')).toBe('10&nbsp;000&nbsp;€ mais `a ; b`\n`10000 ml` 10&nbsp;000&nbsp;ml'); });
});

describe('URLs', () => {
    test('URL must not be changed', () => {
        expect(beautifyTextForFrench('https://www.google.com/search?q=%22javascript%22&sxsrf=ALiCzsbRIobu3xxaBH8wJBdwxbHcvE21KQ%3A1654379567179&ei=L9S')).toBe('https://www.google.com/search?q=%22javascript%22&sxsrf=ALiCzsbRIobu3xxaBH8wJBdwxbHcvE21KQ%3A1654379567179&ei=L9S');
    });
});

describe('Mixes', () => {
    test('Yearn numbers, units', () => {
        expect(beautifyTextForFrench('En 2019, cet État comptait 6 millions d\'habitants, pour une surface d\'environ 180000 km², soit une densité de 34 habitants par km² (la moyenne étant de 33 h/km²).')).toBe('En 2019, cet État comptait 6&nbsp;millions d\'habitants, pour une surface d\'environ 180&nbsp;000&nbsp;km², soit une densité de 34 habitants par km² (la moyenne étant de 33&nbsp;h/km²).');
    });
    test('numbers and units', () => {
        expect(beautifyTextForFrench('Son nom vient de sa cylindrée de 12 x 250 cm³ (3 L)')).toBe('Son nom vient de sa cylindrée de 12 x 250&nbsp;cm³ (3&nbsp;L)');
    });
    test('numbers and units', () => {
        expect(beautifyTextForFrench('Sportif en plein effort : 100 l/min. Bouteille de 5 l gonflée à 200 bars débitant 15 l/min.')).toBe('AAAAAAAASportif en plein effort&nbsp;: 100&nbsp;l/min. Bouteille de 5&nbsp;l gonflée à 200&nbsp;bars débitant 15&nbsp;l/min.');
    });
});
