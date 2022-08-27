/** Observation (idea Simon Treny)
 * 
 * USING
 *  export class TodoService {
 *      readonly todos = new Observable<Todo[]>([]);
 *      readonly visibilityFilter = new Observable(VisibilityFilter.SHOW_ALL);
 *      ...
 * 
 *  export const TodoList = () => {
 *      const todos = useObservable(todoService.todos);
 *      const filter = useObservable(todoService.visibilityFilter);
 *      ...
 *      
 * PLUSES
 * Conciseness: the only thing we had to do is wrap state-values into observables, 
 * and use the useObservable hook when accessing these values from components.
 * 
 * Simplicity: it is now much simpler to trace code execution.
 * 
 * Type-safety: gets TypeScript out of the box. No need to declare types for the state and for every actions.
 * 
 * Async/await: using asynchronous programming a lot easier.
 * 
 * MINUSES
 * Absence: DevTools, infrastructure and etc.
 */

import { useEffect, useState } from "react";
//import { useEffect, useReducer } from "react";

type Listener<T> = (val: T) => void;
type Unsubscriber = () => void;

/** Observable state manager. */
export class Observable<T> {

    private _listeners: Listener<T>[] = [];

    /** Lock or unlock listeners. */
    public lock = false;

    /**
     * Observable for object.
     * @param _value
     */
    constructor(private _value: T) { }

    /** 
     *  View observable value.
     */
    public get view(): T {
        return this._value;
    }
    /**
     * Sets observable value.
     * @param value {T} 
     */
    public set view(value: T) {
        if (this._value !== value) {
            this._value = value;
            if (!this.lock) {
                this._listeners.forEach(l => l(value));
            }
        }
    }

    /**
     * Subscribe for observation.
     * @param listener {Listener<T>} 
     */
    subscribe(listener: Listener<T>): Unsubscriber {
        this._listeners.push(listener);
        return () => {
            this._listeners = this._listeners.filter(l => l !== listener);
        };
    }
}

/**
 * The React hook that adds a state variable to a component, subscribe to the observable
 * and update the state variable when the observable’s value has changed.
 * @param observable object for using in state.
 */
export function useObservable<T>(observable: Observable<T>): T {

    const [value, setVal] = useState(observable.view);
    //const [value, setVal] = useReducer((state, action) => action, observable.view);

    useEffect(() => {
        const s = observable.subscribe(setVal);
        setVal(observable.view);
        return s;
    }, [observable]);

    return value;
}
