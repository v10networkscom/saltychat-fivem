import { TestBed } from '@angular/core/testing';

import { NuiEventService } from './nui-event.service';

describe('NuiEventService', () => {
  let service: NuiEventService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NuiEventService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
