//import * as parser from 'fast-xml-parser';
//import { Base64 } from 'uniya';
import { XmlTextReader, XmlNodeType } from 'uniya-xml';
//import { XmlNode } from 'uniya-xml';
//import { string } from 'prop-types';
//import { has } from 'mobx';
//import { useObservable } from "../model/observation";
//import { useFetch } from "../model/hooks";
//import { rootStore } from "./store";

/**
 * The static core utilities for JavaScript/TypeScript.
 */
export class Localizer {

    // ** fields
    static _path = 'Terms/spa';
    static _terms = new Map<string, Map<string, string>>();
    //private _blockName: string;

    // ** constructor
    constructor(readonly blockName: string) {
    //constructor(readonly culture: string, readonly blockName: string) {
        //
        //let s = Base64.encode("TEXT");
        //this._blockName = blockName;
    }

    // ** properties

    /**
     * Gets whether loaded block terms.
     */
    public get has(): boolean {
        return this.blockName.length > 0 && Localizer._terms.has(this.blockName);
    }

    // ** methods

    /**
     * Gets localized term.
     * @param key {string} The term name.
     */
    public term(key: string): string {
        return Localizer.term(key, this.blockName);
    }

    /**
     * Gets localized terms with prefix.
     * @param prefix {string} The prefix of the term for search.
     */
    public terms(prefix: string): Map<string, string> {
        const result = new Map<string, string>();
        if (Localizer._terms.has(this.blockName)) {
            const map = Localizer._terms.get(this.blockName) as Map<string, string>;
            for (const [key, value] of map) {
                if (key.startsWith(prefix)) {
                    result.set(key.substr(prefix.length), value);
                }
            }
        }
        return result;
    }

    /**
     * Load terms for component (dialog) block.
     */
    public async load() {

        if (this.blockName.length > 0) {
            await Localizer.load(this.blockName);
        }
    }

    // ** static methods

    /**
     * Gets localized term.
     * @param key {string} The term name.
     * @param block {string} The block name, by default common block.
     */
    public static term(key: string, block = ""): string {

        // initialization
        let map: Map<string, string>;

        // component (dialog) block
        if (block.length > 0 && Localizer._terms.has(block)) {
            map = Localizer._terms.get(block) as Map<string, string>;
            if (map.size > 0 && map.has(key)) {
                return map.get(key) as string;
            }
        }

        // common
        if (Localizer._terms.has("")) {
            map = Localizer._terms.get("") as Map<string, string>;
            if (map.size > 0 && map.has(key)) {
                return map.get(key) as string;
            }
        }

        // bad done
        return key;
    }

    public static unescaping(text: string): string {

        return text.replace(/(&amp;|&lt;|&gt;|&quot;|&#39;|&#x2F;)/g, function (ch) {
            switch (ch) {
                case "&amp;": return "&";
                case "&lt;": return "<";
                case "&gt;": return ">";
                case "&quot;": return "\"";
                case "&#39;": return "'";
                case "&#x2F;": return "/";
                case "&nbsp;": return " ";
            }
            return ch;
        });
    }

    /**
     * 
     * @param block {string} The block name, by default all blocks.
     */
    public static async load(block = "") {

        try {

            // clear for default
            if (block.length === 0) {
                Localizer._terms.clear();
                //return;
            }

            // fetch
            const response = await fetch(Localizer._path);
            if (!response.ok) {
                throw Error(response.statusText);
            }
            const xml = await response.text();
            //xml = '<Header><DocDate>28.07.2016</DocDate></Header>';
            //let node = XmlNode.parse(xml);
            //let text = node.toXMLString();
            //console.log(text.substr(0, 40));

            // initialization
            const xr = new XmlTextReader(xml);
            let foundBlock = false;
            let termName = "";

            // read each node
            while (xr.read()) {

                switch (xr.nodeType) {
                    case XmlNodeType.Element:
                        switch (xr.name) {
                            case "data":
                                if (xr.hasAttributes) {
                                    for (const [key, value] of xr.attributes) {
                                        if (key === "name") {
                                            termName = value;
                                            break;
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    case XmlNodeType.Text:
                        if (termName.length > 0) {

                            // detect block
                            const ch = termName.charAt(0);
                            const pos = (ch === '@') ? termName.indexOf('_') : 0;
                            const blockName = (pos > 0) ? termName.substr(1, pos - 1) : "";

                            // correct term value
                            if (pos > 0 && termName.length > pos + 1) {
                                termName = termName.substr(pos + 1);
                            }

                            // if load one block?
                            if (block.length > 0) {
                                if (foundBlock && block !== blockName) {

                                    // break done
                                    return;
                                }
                                foundBlock = (block === blockName);
                            }

                            // current map
                            let map: Map<string, string>;
                            if (Localizer._terms.has(blockName)) {
                                map = Localizer._terms.get(blockName) as Map<string, string>;
                            } else {
                                map = new Map<string, string>();
                                Localizer._terms.set(blockName, map);
                            }

                            // decoding
                            let idx = 0;
                            let value = Localizer.unescaping(xr.value);
                            while (idx < value.length) {
                                const begin = value.indexOf('&#x', idx);
                                if (begin !== -1) {
                                    const end = value.indexOf(';', begin);
                                    if (end !== -1) {
                                        let code = value.substr(begin + 3, end - begin - 3);
                                        while (code.length < 4) {
                                            code = '0' + code;
                                        }
                                        value = value.substr(0, begin) + unescape('%u' + code) + value.substr(end + 1);
                                    }
                                    idx = begin;
                                }
                                idx++;
                            }

                            // set
                            map.set(termName, value);
                            termName = "";
                        }
                        break;
                }
            }

            //var options = {
            //    attributeNamePrefix: '',
            //    ignoreAttributes: false,
            //    ignoreNameSpace: false,
            //    allowBooleanAttributes: false,
            //    parseNodeValue: true,
            //    parseAttributeValue: false,
            //    trimValues: true,
            //    localeRange: '',
            //    parseTrueNumberOnly: false
            //};

            //let obj = parser.parse(data, options);

            //if (typeof (obj.root) === 'object' && typeof (obj.root.data) === 'object') {

            //    let list = obj.root.data;

            //    for (let i = 0; i < list.length; i++) {

            //        let name = list[i].name;
            //        let value = list[i].value;

            //        if (name.length > 0 && !Localizer._phrases.has(name) && value != null) {

            //            // decoding
            //            let idx = 0;
            //            while (idx < value.length) {
            //                let begin = value.indexOf('&#x', idx);
            //                if (begin !== -1) {
            //                    let end = value.indexOf(';', begin);
            //                    if (end !== -1) {
            //                        let code = value.substr(begin + 3, end - begin - 3);
            //                        while (code.length < 4) {
            //                            code = '0' + code;
            //                        }
            //                        value = value.substr(0, begin) + unescape('%u' + code) + value.substr(end + 1);
            //                    }
            //                    idx = begin;
            //                }
            //                idx++;
            //            }

            //            // pair
            //            Localizer._phrases.set(name, value);
            //        }
            //    }
            //}
        } catch (error) {
            console.log(error);
            //_phrases[0] = 0;
        }
    }
}
