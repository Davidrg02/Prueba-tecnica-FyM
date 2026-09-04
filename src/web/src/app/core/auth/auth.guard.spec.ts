import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UrlTree, provideRouter } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  let authServiceStub: Partial<AuthService>;

  function configure(isAuthenticated: boolean, hasPermission = false): void {
    authServiceStub = {
      isAuthenticated: signal(isAuthenticated),
      hasPermission: () => hasPermission,
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceStub }],
    });
  }

  it('authGuard allows navigation when authenticated', () => {
    configure(true);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/users' } as never),
    );
    expect(result).toBeTrue();
  });

  it('authGuard redirects to /login with returnUrl when not authenticated', () => {
    configure(false);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/users' } as never),
    ) as UrlTree;
    expect(result.toString()).toContain('/login');
    expect(result.toString()).toContain('returnUrl');
  });

  it('guestGuard blocks an already-authenticated user from /login', () => {
    configure(true);
    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never)) as UrlTree;
    expect(result.toString()).toBe('/');
  });

  it('guestGuard allows anonymous users through', () => {
    configure(false);
    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
    expect(result).toBeTrue();
  });

  it('permissionGuard allows a user who has the required permission', () => {
    configure(true, true);
    const guard = permissionGuard('users.read');
    const result = TestBed.runInInjectionContext(() => guard({} as never, { url: '/users' } as never));
    expect(result).toBeTrue();
  });

  it('permissionGuard redirects to /forbidden when the permission is missing', () => {
    configure(true, false);
    const guard = permissionGuard('users.read');
    const result = TestBed.runInInjectionContext(() =>
      guard({} as never, { url: '/users' } as never),
    ) as UrlTree;
    expect(result.toString()).toBe('/forbidden');
  });
});
