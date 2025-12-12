# Google Authentication Setup

This document describes how to set up Google OAuth 2.0 authentication for the FishyBackendAPI.

## Prerequisites

1. A Google Cloud Platform (GCP) project
2. OAuth 2.0 credentials configured in GCP Console

## Steps to Configure Google Authentication

### 1. Create OAuth 2.0 Credentials in Google Cloud Console

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth 2.0 Client ID**
5. Configure the OAuth consent screen if you haven't already
6. Select **Web application** as the application type
7. Add the following authorized redirect URIs:
   - `https://api.fishnation.xyz/auth/google/callback`
   - For local development: `http://localhost/auth/google/callback`
8. Click **Create**
9. Save the **Client ID** and **Client Secret**

### 2. Configure Environment Variables

Add the following environment variables to your `.env` file:

```bash
GOOGLE_CLIENT_ID=your_google_client_id_here
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
```

### 3. Run Database Migration

Execute the SQL migration to add Google authentication fields to the users table:

```bash
mysql -h [host] -u [user] -p [database] < Migrations/001_add_google_auth_fields.sql
```

Or manually run:

```sql
ALTER TABLE users 
ADD COLUMN GoogleID VARCHAR(255) NULL AFTER DiscordName,
ADD COLUMN GoogleEmail VARCHAR(255) NULL AFTER GoogleID;

CREATE INDEX idx_google_id ON users(GoogleID);
```

### 4. API Endpoints

Once configured, the following endpoints will be available:

- **GET /Auth/Google** - Initiates the Google OAuth flow
  - Redirects the user to Google's authentication page
  
- **GET /Auth/Google/Callback** - Handles the OAuth callback
  - Exchanges the authorization code for user information
  - Creates or retrieves the user from the database
  - Generates a JWT token
  - Redirects to the frontend with the token

### 5. User Flow

1. Frontend redirects user to `/Auth/Google`
2. User authenticates with Google and grants permissions
3. Google redirects back to `/Auth/Google/Callback` with authorization code
4. Backend exchanges code for user information
5. Backend creates a new user or retrieves existing user
6. Backend generates JWT token
7. Backend redirects to `https://fishnation.xyz/auth/google/callback?token={jwt_token}`
8. Frontend stores the token and authenticates subsequent requests

## Security Considerations

- Google Client Secret should be kept confidential and never committed to version control
- OAuth redirect URIs must match exactly with what's configured in Google Cloud Console
- JWT tokens expire after 2 hours (configurable in `Tokenizer.cs`)
- User profile pictures and emails are obtained from Google's user info endpoint

## Testing

To test the authentication flow:

1. Navigate to `https://api.fishnation.xyz/Auth/Google` in your browser
2. Complete the Google authentication
3. You should be redirected to the frontend with a valid JWT token
4. The token can be decoded to verify user claims (uid, google_id, role, etc.)
