import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeviceBodyComponent } from './device-body.component';

describe('DeviceBodyComponent', () => {
  let component: DeviceBodyComponent;
  let fixture: ComponentFixture<DeviceBodyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DeviceBodyComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DeviceBodyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
