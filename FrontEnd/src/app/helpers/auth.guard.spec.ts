import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('should be defined', () => {
    expect(authGuard).toBeTruthy();
  });
});
