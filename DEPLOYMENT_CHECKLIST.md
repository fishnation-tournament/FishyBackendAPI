# Google Authentication Deployment Checklist

This checklist should be completed by the system administrator to deploy Google Authentication.

## Prerequisites
- [ ] Access to Google Cloud Platform Console
- [ ] Access to production database
- [ ] Access to server environment variables

## Step 1: Google Cloud Configuration
- [ ] Create or select a GCP project
- [ ] Navigate to **APIs & Services** > **Credentials**
- [ ] Create OAuth 2.0 Client ID (Web application)
- [ ] Add authorized redirect URI: `https://api.fishnation.xyz/auth/google/callback`
- [ ] For local testing, also add: `http://localhost/auth/google/callback`
- [ ] Save the Client ID and Client Secret

## Step 2: Environment Variables
Add the following to your `.env` file or environment configuration:

```bash
GOOGLE_CLIENT_ID=your_google_client_id_here
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
```

- [ ] Environment variables added to production server
- [ ] Environment variables added to development/staging (if applicable)

## Step 3: Database Migration
Run the SQL migration script to add Google authentication fields:

```bash
mysql -h [host] -u [user] -p [database] < Migrations/001_add_google_auth_fields.sql
```

Or manually execute:

```sql
ALTER TABLE users 
ADD COLUMN GoogleID VARCHAR(255) NULL AFTER DiscordName,
ADD COLUMN GoogleEmail VARCHAR(255) NULL AFTER GoogleID;

CREATE INDEX idx_google_id ON users(GoogleID);
```

- [ ] Migration executed on production database
- [ ] Migration executed on development/staging database (if applicable)

## Step 4: Deploy Code
- [ ] Code deployed to production server
- [ ] Server restarted to load new environment variables
- [ ] Verify no build errors in deployment logs

## Step 5: Testing
- [ ] Test authentication flow: Navigate to `https://api.fishnation.xyz/Auth/Google`
- [ ] Verify redirect to Google login page
- [ ] Complete authentication
- [ ] Verify redirect back to frontend with valid JWT token
- [ ] Decode JWT token to verify it contains `google_id` claim
- [ ] Test with new user (should create user in database)
- [ ] Test with existing user (should retrieve existing user)
- [ ] Verify user data in database includes GoogleID and GoogleEmail

## Step 6: Frontend Integration
- [ ] Update frontend to include "Sign in with Google" button
- [ ] Button should link to `/Auth/Google` endpoint
- [ ] Frontend handles token callback at `/auth/google/callback?token={jwt}`
- [ ] Frontend stores token and uses for authenticated requests

## Troubleshooting

### Common Issues

**Redirect URI Mismatch**
- Error: `redirect_uri_mismatch`
- Solution: Ensure the redirect URI in Google Cloud Console exactly matches `https://api.fishnation.xyz/auth/google/callback`

**Invalid Client**
- Error: `invalid_client`
- Solution: Verify GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables are correct

**Database Error**
- Error: Column not found or SQL errors
- Solution: Verify the migration script was executed successfully and columns exist in the users table

**No Token Received**
- Error: Frontend doesn't receive token
- Solution: Check server logs for errors in the callback handler

## Documentation
Detailed setup instructions can be found in `GOOGLE_AUTH_SETUP.md`

## Support
For issues or questions, refer to the main repository documentation or contact the development team.
