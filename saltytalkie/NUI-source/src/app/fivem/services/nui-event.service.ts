import { Injectable } from '@angular/core';
import { fromEvent, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { NuiMessage } from '../models/nui-message.interface';

// @ts-ignore
if (!window.GetParentResourceName) {
  // @ts-ignore
  window[`GetParentResourceName`] = () => {
    return 'filePath';
  };
}


@Injectable({
  providedIn: 'root'
})
export class NuiEventService {
  private readonly _messages$: Observable<NuiMessage>;


  constructor(private httpClient: HttpClient) {
    this._messages$ = fromEvent<any>(window, 'message')
      .pipe(
        map(
          eventMessage => eventMessage.data
        )
      );
  }


  get parentResourceName(): string {
    // @ts-ignore
    return window.GetParentResourceName() ?? 'unknown';
  }

  postRequest<T = any>(eventName: string, body?: any, parseJSON = false): Observable<T> {
    return this.httpClient.post<string>(`http://${this.parentResourceName}/` + eventName, body).pipe(
      map(values => parseJSON ? JSON.parse(values) : values)
    );
  }

  messages$(): Observable<NuiMessage> {
    return this._messages$;
  }
}
