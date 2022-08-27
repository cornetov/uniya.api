/**
 * The static core utilities for JavaScript/TypeScript.
 */
export class Core {

    /**
     * Sleep.
     * @param ms The milliseconds of wait.
     */
    static async sleep(ms: number) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Is Node.js environment or no
     */
    public static get isNode(): boolean {
        if (typeof process === 'object') {
            if (typeof process.versions === 'object') {
                if (typeof process.versions.node !== 'undefined') {
                    return true;
                }
            }
        }
        return false;
    }

    /**
     * Is empty object?
     * @param a The object for test.
     */
    public static isEmpty(a: any) {
        try {
            return Object.keys(a).length === 0;
        }
        catch (ex) {

            // unexpected exception!
            console.log(ex);

            // bad done
            return false;
        }
    }
    /**
     * Create clone object.
     * @param a The object to clone.
     */
    public static clone(a: any) {
        try {
            return JSON.parse(JSON.stringify(a));
        }
        catch (ex) {

            // unexpected exception!
            console.log(ex);

            // basis values
            if (!(a instanceof Object)) {
                return a;
            }

            // special objects
            var objectClone;
            var Constructor = a.constructor;
            switch (Constructor) {
                case RegExp:
                    objectClone = new Constructor(a);
                    break;
                case Date:
                    objectClone = new Constructor(a.getTime());
                    break;
                default:
                    objectClone = new Constructor();
            }

            // each property
            for (let p in a) {
                objectClone[p] = Core.clone(a[p]);
            }

            // done
            return objectClone;
        }
    }
    /**
     * Create typed clone object.
     * @param a The object to clone.
     */
    public static typedClone<T>(a: any): T {

        // special objects
        let clone: T;
        let Constructor = a.constructor;
        switch (Constructor) {
            case RegExp:
                clone = new Constructor(a);
                break;
            case Date:
                clone = new Constructor(a.getTime());
                break;
            default:
                clone = new Constructor();
        }

        // each property
        for (let p in a) {
            //clone.p = Core.clone(a[p]);

            //let obj = clone as {};
            //if (obj !== null) {
            //    obj[p] = Core.clone(a[p]);
            //}
            //if (clone instanceof Object) {
            //    clone[p] = Core.clone(a[p]);
            //}
        }

        // done
        return clone;
    }
    /**
     * Repeat string or char for text.
     * @param s The char or sring for a repeat.
     * @param count How many times is need to repeat.
     * @param text The text for added, by default is empty.
     * @return {string} The text with repeated blocks.
     */
    public static repeater(s: string, count: number, text: string = ""): string {
        for (let i = 0; i < Math.max(0, count); i++) {
            text += s;
        }
        return text;
    }
}

/**
 * Base exception class.
 * @class Exception
 */
export class Base64 {

    /**
     * Convert ucs-2 string to base64 encoded ascii
     * @param str {string} The common ucs-2 string.
     */
    public static encode(str: string): string {

        if (Core.isNode) {
            let buffer = new Buffer(str, 'utf8');
            return buffer.toString('base64');
        }

        //Hello World
        //SGVsbG8gV29ybGQ=
        return btoa(unescape(encodeURIComponent(str)));
    }

    /**
     * Convert base64 encoded ascii to ucs-2 string
     * @param str {string} The base64 encoded ascii.
     */
    public static decode(str: string): string {

        if (Core.isNode) {
            let buffer = new Buffer(str, 'base64');
            return buffer.toString('utf8');
        }

        //> console.log(Buffer.from("SGVsbG8gV29ybGQ=", 'base64').toString('ascii'))
        //Hello World
        return decodeURIComponent(escape(atob(str)));
    }
}
