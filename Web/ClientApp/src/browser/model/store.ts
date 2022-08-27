import { Observable } from "./observation";

export enum SupportedCulture {
    AUTO = "",
    ENGLISH = "en-US",
    RUSSIAN = "ru-RU",
}

export interface Todo {
    readonly text: string;
    readonly completed: boolean;
}

export enum VisibilityFilter {
    SHOW_ALL,
    SHOW_COMPLETED,
    SHOW_ACTIVE,
}

export interface CultureTerm {
    readonly term: string;
    readonly value: string;
}

/** The root store of the solution. */
export class RootStore {
    //read-only cultureTerms = new Observable < Record< CultureTerm[]> ([]);
    private _culture = new Observable<SupportedCulture>(SupportedCulture.AUTO);
    private readonly _terms = new Observable(new Map<string, string>());

    /** 
     * Gets current culture.
     */
    public get culture(): SupportedCulture {
        return this._culture.view;
    }
    /**
     * Sets current culture.
     * @param value {T} 
     */
    public set culture(value: SupportedCulture) {
        if (this._culture.view !== value) {

            // reload culture terms
            //const loca
            //_terms.

            // change culture
            this._culture.view = value;
        }
    }

    /**
     * Gets culture (language) term value.
     * @param term {string} The unique term.
     */
    public getTerm(term: string): string {
        let value = term;
        if (this._terms.view.has(term)) {
            const v = this._terms.view.get(term);
            if (v !== undefined) {
                value = v;
            }
        }
        return value;
    }
    /**
     * Sets culture (language) term value.
     * @param term {string} The unique term.
     * @param value {string} The text for term or null for delete term.
     */
    public setTerm(term: string, value: string | null) {
        if (term.length > 0) {
            if (value === null) {
                if (this._terms.view.has(term)) {
                    this._terms.view.delete(term);
                }
            } else {
                this._terms.view.set(term, value);
            }
        }
    }

    readonly todos = new Observable<Todo[]>([]);
    readonly visibilityFilter = new Observable(VisibilityFilter.SHOW_ALL);

    addTodo(text: string) {
        this.todos.view = [...this.todos.view, { text, completed: false }];
    }

    toggleTodo(index: number) {
        this.todos.view = this.todos.view.map(
            (todo, i) => (i === index ? { text: todo.text, completed: !todo.completed } : todo)
        );
    }

    setVisibilityFilter(filter: VisibilityFilter) {
        this.visibilityFilter.view = filter;
    }
}

export const rootStore = new RootStore();

/*
import { Observable } from "./observation";

export interface Weather {
    weatherDate: Date;
    temperatureF: number;
    temperatureC: number;
    summary: string;
}

export interface LanguageTerms {
    culture: string;
    temperatureF: number;
    temperatureC: number;
    summary: string;
}


export interface Todo {
    readonly text: string;
    readonly completed: boolean;
}

export enum VisibilityFilter {
    SHOW_ALL,
    SHOW_COMPLETED,
    SHOW_ACTIVE,
}

export class TodoService {
    readonly todos = new Observable<Todo[]>([]);
    readonly visibilityFilter = new Observable(VisibilityFilter.SHOW_ALL);

    addTodo(text: string) {
        this.todos.set([...this.todos.get(), { text, completed: false }]);
    }

    toggleTodo(index: number) {
        this.todos.set(this.todos.get().map(
            (todo, i) => (i === index ? { text: todo.text, completed: !todo.completed } : todo)
        ));
    }

    setVisibilityFilter(filter: VisibilityFilter) {
        this.visibilityFilter.set(filter);
    }
}
*/

//import { observable, action, computed } from 'mobx';

//export interface IRootStore {
//    name: string;
//    greeting: string;
//    setName(name: string): void;
//}

//export class RootStore implements IRootStore {
//    @observable name = "World";

//    @computed
//    public get greeting(): string {
//        return `Hello ${this.name}`;
//    }

//    @action.bound
//    public setName(name: string): void {
//        this.name = name;
//    }
//}

//export const rootStore = new RootStore()
