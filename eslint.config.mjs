import { defineConfig } from "eslint/config";
import globals from "globals";

export default defineConfig([{
    languageOptions: {
        globals: {
            ...globals.browser,
            ...Object.fromEntries(Object.entries(globals.node).map(([key]) => [key, "off"])),
        },

        ecmaVersion: 2020,
        sourceType: "module",
    },

    rules: {
        strict: ["error", "global"],

        "no-unused-vars": ["error", {
            vars: "all",
            args: "all",
            argsIgnorePattern: "^_",
        }],

        "comma-dangle": "off",
        "no-alert": "off",
        curly: "off",

        "dot-notation": ["error", {
            allowKeywords: true,
        }],

        indent: ["error", 4, {
            SwitchCase: 1,
        }],

        "brace-style": ["error", "stroustrup", {
            allowSingleLine: true,
        }],

        quotes: ["error", "single", "avoid-escape"],

        "comma-spacing": ["error", {
            before: false,
            after: true,
        }],

        "keyword-spacing": "error",
        "space-before-blocks": "error",
        "space-before-function-paren": ["error", "never"],
        "spaced-comment": "error",
        "no-extra-parens": "off",
        radix: "off",
        "prefer-template": "error",
        "no-console": "error",
        "no-nested-ternary": "off",
        "no-var": 2,
        "no-shadow": 2,
        "no-shadow-restricted-names": 2,

        "no-use-before-define": ["error", {
            variables: false,
        }],

        "no-cond-assign": [2, "always"],
        "no-debugger": 1,
        "no-constant-condition": 1,
        "no-dupe-keys": 2,
        "no-duplicate-case": 2,
        "no-empty": 2,
        "no-ex-assign": 2,
        "no-extra-boolean-cast": 0,
        "no-extra-semi": 2,
        "no-func-assign": 2,
        "no-inner-declarations": 2,
        "no-invalid-regexp": 2,
        "no-irregular-whitespace": 2,
        "no-obj-calls": 2,
        "no-sparse-arrays": 2,
        "no-unreachable": 2,
        "use-isnan": 2,
        "block-scoped-var": 2,
        "consistent-return": 2,
        "default-case": 2,
        eqeqeq: 2,
        "guard-for-in": 2,
        "no-caller": 2,
        "no-else-return": 2,
        "no-eq-null": 2,
        "no-eval": 2,
        "no-extend-native": 2,
        "no-extra-bind": 2,
        "no-fallthrough": 2,
        "no-floating-decimal": 2,
        "no-implied-eval": 2,
        "no-lone-blocks": 2,
        "no-loop-func": 2,
        "no-multi-str": 2,
        "no-native-reassign": 2,
        "no-new": 2,
        "no-new-func": 2,
        "no-new-wrappers": 2,
        "no-octal": 2,
        "no-octal-escape": 2,
        "no-param-reassign": 2,
        "no-proto": 2,
        "no-redeclare": 2,
        "no-return-assign": 2,
        "no-script-url": 2,
        "no-self-compare": 2,
        "no-sequences": 2,
        "no-throw-literal": 2,
        "no-with": 2,
        "vars-on-top": 2,
        "wrap-iife": [2, "any"],
        yoda: 2,

        camelcase: [2, {
            properties: "never",
        }],

        "comma-style": [2, "last"],
        "eol-last": 2,
        "func-names": 1,

        "key-spacing": [2, {
            beforeColon: false,
            afterColon: true,
        }],

        "new-cap": [2, {
            newIsCap: true,
        }],

        "no-multiple-empty-lines": [2, {
            max: 2,
        }],

        "no-new-object": 2,
        "no-spaced-func": 2,
        "no-trailing-spaces": 2,
        "no-underscore-dangle": 0,
        "one-var": [2, "never"],
        "padded-blocks": [2, "never"],
        semi: [2, "always"],

        "semi-spacing": [2, {
            before: false,
            after: true,
        }],

        "space-infix-ops": "error",
    },
}]);