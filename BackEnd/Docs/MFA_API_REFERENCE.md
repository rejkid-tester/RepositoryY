# MFA API Quick Reference

## ?? Authentication Endpoints

### Login (without MFA)
```http
POST /api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```
**Success Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpc2lzdGhlcmVmc...",
  "userId": 123,
  "firstName": "John"
}
```

---

### Login (with MFA enabled)
```http
POST /api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```
**MFA Required Response:**
```json
{
  "success": true,
  "mfaRequired": true,
  "userId": 123
}
```
*?? 6-digit SMS code sent to user's phone*

---

### Verify MFA Code
```http
POST /api/users/verify-mfa
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "123456"
}
```
**Success Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpc2lzdGhlcmVmc...",
  "userId": 123,
  "firstName": "John"
}
```

**Error Responses:**
```json
// Invalid code
{
  "success": false,
  "error": "Invalid MFA code",
  "errorCode": "MFA02"
}

// Expired code
{
  "success": false,
  "error": "MFA code expired",
  "errorCode": "MFA03"
}
```

---

## ?? MFA Management Endpoints

### Enable MFA
```http
POST /api/users/enable-mfa
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "phoneNumber": "+61412345678"
}
```
**Success Response:**
```json
{
  "success": true,
  "message": "MFA enabled successfully"
}
```

**Phone Number Format Requirements:**
- ? E.164 format required (+country_code)
- ? Examples: `+61412345678`, `+14155552671`, `+442071838750`
- ? Invalid: `0412345678`, `4155552671`

---

### Disable MFA
```http
POST /api/users/disable-mfa
Authorization: Bearer {access_token}
```
**Success Response:**
```json
{
  "success": true,
  "message": "MFA disabled successfully"
}
```

---

## ?? Error Codes Reference

| Code   | Description                          | Endpoint        |
|--------|--------------------------------------|-----------------|
| L02    | Email not found                      | login           |
| L03    | Invalid password                     | login           |
| MFA01  | Invalid MFA request                  | verify-mfa      |
| MFA02  | Invalid MFA code                     | verify-mfa      |
| MFA03  | MFA code expired (>5 min)           | verify-mfa      |
| MFA04  | User not found (enable)             | enable-mfa      |
| MFA05  | User not found (disable)            | disable-mfa     |

---

## ?? Complete MFA Flow Example

### Step 1: User Enables MFA
```javascript
// User settings page
const enableMFA = async () => {
  const response = await fetch('/api/users/enable-mfa', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      phoneNumber: '+61412345678'
    })
  });
  
  const data = await response.json();
  console.log(data.message); // "MFA enabled successfully"
};
```

### Step 2: User Logs In (Triggers SMS)
```javascript
// Login page
const login = async (email, password) => {
  const response = await fetch('/api/users/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  
  const data = await response.json();
  
  if (data.mfaRequired) {
    // Redirect to MFA verification page
    return { needsMFA: true, userId: data.userId };
  } else {
    // Normal login - store tokens
    localStorage.setItem('accessToken', data.accessToken);
    return { needsMFA: false };
  }
};
```

### Step 3: User Verifies Code
```javascript
// MFA verification page
const verifyMFA = async (email, code) => {
  const response = await fetch('/api/users/verify-mfa', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, code })
  });
  
  const data = await response.json();
  
  if (data.success) {
    // Store tokens and redirect to dashboard
    localStorage.setItem('accessToken', data.accessToken);
    window.location.href = '/dashboard';
  } else {
    // Show error message
    alert(data.error);
  }
};
```

---

## ?? Frontend Components (React Example)

### MFA Verification Component
```jsx
import React, { useState } from 'react';

const MFAVerification = ({ email, onVerified }) => {
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/users/verify-mfa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code })
      });

      const data = await response.json();

      if (data.success) {
        localStorage.setItem('accessToken', data.accessToken);
        onVerified();
      } else {
        setError(data.error);
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mfa-verification">
      <h2>Verify Your Identity</h2>
      <p>Enter the 6-digit code sent to your phone</p>
      
      <form onSubmit={handleSubmit}>
        <input
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder="000000"
          maxLength="6"
          pattern="[0-9]{6}"
          required
          autoFocus
        />
        
        {error && <div className="error">{error}</div>}
        
        <button type="submit" disabled={loading || code.length !== 6}>
          {loading ? 'Verifying...' : 'Verify'}
        </button>
      </form>
      
      <p className="hint">Code expires in 5 minutes</p>
    </div>
  );
};

export default MFAVerification;
```

### Enable MFA Component
```jsx
import React, { useState } from 'react';

const EnableMFA = ({ accessToken, onEnabled }) => {
  const [phoneNumber, setPhoneNumber] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleEnable = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/users/enable-mfa', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ phoneNumber })
      });

      const data = await response.json();

      if (data.success) {
        onEnabled();
      } else {
        setError(data.error);
      }
    } catch (err) {
      setError('Failed to enable MFA. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="enable-mfa">
      <h3>Enable Two-Factor Authentication</h3>
      <p>Add an extra layer of security to your account</p>
      
      <form onSubmit={handleEnable}>
        <label>
          Phone Number (with country code)
          <input
            type="tel"
            value={phoneNumber}
            onChange={(e) => setPhoneNumber(e.target.value)}
            placeholder="+61412345678"
            required
          />
        </label>
        
        {error && <div className="error">{error}</div>}
        
        <button type="submit" disabled={loading}>
          {loading ? 'Enabling...' : 'Enable MFA'}
        </button>
      </form>
    </div>
  );
};

export default EnableMFA;
```

---

## ?? Postman Collection

Import this into Postman for easy testing:

```json
{
  "info": {
    "name": "MFA API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Login",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"email\": \"user@example.com\",\n  \"password\": \"password123\"\n}"
        },
        "url": "{{baseUrl}}/api/users/login"
      }
    },
    {
      "name": "Verify MFA",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"email\": \"user@example.com\",\n  \"code\": \"123456\"\n}"
        },
        "url": "{{baseUrl}}/api/users/verify-mfa"
      }
    },
    {
      "name": "Enable MFA",
      "request": {
        "method": "POST",
        "header": [
          {"key": "Content-Type", "value": "application/json"},
          {"key": "Authorization", "value": "Bearer {{accessToken}}"}
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"phoneNumber\": \"+61412345678\"\n}"
        },
        "url": "{{baseUrl}}/api/users/enable-mfa"
      }
    },
    {
      "name": "Disable MFA",
      "request": {
        "method": "POST",
        "header": [
          {"key": "Authorization", "value": "Bearer {{accessToken}}"}
        ],
        "url": "{{baseUrl}}/api/users/disable-mfa"
      }
    }
  ]
}
```

---

## ?? Testing Checklist

- [ ] Can enable MFA with valid phone number
- [ ] Receive SMS with 6-digit code
- [ ] Can verify code and get access token
- [ ] Invalid code returns error MFA02
- [ ] Expired code (>5 min) returns error MFA03
- [ ] Can disable MFA
- [ ] Login without MFA works normally
- [ ] Login with MFA triggers SMS
- [ ] Code is cleared after successful verification
- [ ] Cannot verify code twice
