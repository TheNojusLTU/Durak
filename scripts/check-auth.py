"""Run against an API configured with a disposable database; creates two test users.

Usage: python3 scripts/check-auth.py http://localhost:5099 --allow-test-users
Uses only the Python standard library. Does not print passwords or cookie/token values.
"""

import argparse
import http.cookiejar
import json
import secrets
import urllib.error
import urllib.request


class Client:
    def __init__(self, base_url):
        self.base_url = base_url.rstrip('/')
        self.cookies = http.cookiejar.CookieJar()
        self.http = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.cookies), NoRedirects())
        self.token = None

    def request(self, method, path, expected, payload=None, csrf=True):
        headers = {'Accept': 'application/json'}
        if csrf and self.token:
            headers['X-CSRF-TOKEN'] = self.token
        data = None
        if payload is not None:
            headers['Content-Type'] = 'application/json'
            data = payload if isinstance(payload, bytes) else json.dumps(payload).encode()
        request = urllib.request.Request(self.base_url + '/api/auth' + path,
                                         data=data, headers=headers, method=method)
        try:
            response = self.http.open(request, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        body = response.read()
        assert response.status == expected, f'{method} {path}: expected {expected}, got {response.status}'
        result = json.loads(body) if body else None
        if expected >= 400:
            assert response.headers.get_content_type() == 'application/problem+json'
            assert result['status'] == expected
        print(f'PASS {method} {path}: {expected}')
        return result, response.headers

    def csrf(self):
        result, headers = self.request('GET', '/csrf', 200)
        assert result['headerName'] == 'X-CSRF-TOKEN' and result['requestToken']
        assert 'no-cache' in headers['Cache-Control'] or 'no-store' in headers['Cache-Control']
        self.token = result['requestToken']


class NoRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def profile(result, username, email=None, avatar='sunrise'):
    assert set(result) == {'id', 'username', 'displayName', 'email', 'avatarKey'}
    assert result['id'] and result['username'] == username
    assert result['displayName'] == 'API Tester'
    assert result['email'] == email and result['avatarKey'] == avatar


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('base_url')
    parser.add_argument('--allow-test-users', required=True, action='store_true',
                        help='Confirm this API uses a disposable test database.')
    args = parser.parse_args()
    client = Client(args.base_url)
    username = 'api' + secrets.token_hex(6)
    password = 'Aa1!' + secrets.token_urlsafe(24)
    registration = {'username': username, 'displayName': 'API Tester', 'password': password}

    client.request('GET', '/me', 401)
    client.request('POST', '/logout', 401, {})
    client.request('POST', '/register', 400, registration, csrf=False)
    client.request('POST', '/login', 400, {'username': username, 'password': password}, csrf=False)
    client.csrf()

    for path in ('/register', '/login'):
        for invalid in ([], b'{', b'null'):
            client.request('POST', path, 400, invalid)

    for values, field in [({'username': 'ab'}, 'username'),
                          ({'username': 'a' * 21}, 'username'),
                          ({'username': 'bad name'}, 'username'),
                          ({'displayName': 'ab'}, 'displayName'),
                          ({'email': 'invalid'}, 'email'),
                          ({'avatarKey': 'invalid'}, 'avatarKey'),
                          ({'password': 'weak'}, 'password'),
                          ({'password': None}, 'password')]:
        problem, _ = client.request('POST', '/register', 400, registration | values)
        assert field in problem['errors']
    client.request('POST', '/login', 400, {})

    user, headers = client.request('POST', '/register', 201, registration)
    profile(user, username)
    assert headers['Location'] == '/api/auth/me'
    assert 'no-store' in headers['Cache-Control']
    assert any(cookie.name == 'JobTracker.Auth' and any(key.lower() == 'httponly' for key in cookie._rest)
               and cookie.discard for cookie in client.cookies)
    profile(client.request('GET', '/me', 200)[0], username)

    # Anonymous tokens are invalid after authentication. Failed CSRF checks must not log out.
    client.request('POST', '/logout', 400, {})
    client.request('POST', '/logout', 400, {}, csrf=False)
    client.csrf()
    correct_token = client.token
    client.token = 'invalid'
    client.request('POST', '/logout', 400, {})
    other = Client(args.base_url)
    other.csrf()
    client.token = other.token
    client.request('POST', '/logout', 400, {})
    client.token = correct_token
    profile(client.request('GET', '/me', 200)[0], username)

    # Username uniqueness is case-insensitive, and email is optional.
    client.request('POST', '/register', 409, registration | {'username': username.upper()})
    client.request('POST', '/logout', 204, {})
    client.request('GET', '/me', 401)
    client.request('POST', '/logout', 401, {})
    client.csrf()

    wrong, _ = client.request('POST', '/login', 401, {'username': username, 'password': password + 'wrong'})
    missing, _ = client.request('POST', '/login', 401, {'username': 'missing' + secrets.token_hex(6), 'password': password})
    assert wrong['title'] == missing['title'] == 'Invalid username or password.'
    client.request('GET', '/me', 401)
    user, _ = client.request('POST', '/login', 200, {'username': username.upper(), 'password': password})
    profile(user, username)
    profile(client.request('GET', '/me', 200)[0], username)
    client.csrf()
    client.request('POST', '/logout', 204, {})
    client.request('GET', '/me', 401)

    # Optional email and an explicit avatar round-trip through Identity.
    other_name = 'api' + secrets.token_hex(6)
    email = other_name + '@example.test'
    other.csrf()
    user, _ = other.request('POST', '/register', 201, registration | {
        'username': other_name, 'email': email, 'avatarKey': 'ocean'})
    profile(user, other_name, email, 'ocean')
    other.csrf()
    other.request('POST', '/logout', 204, {})
    print('All authentication checks passed; two test users remain in the disposable database.')


if __name__ == '__main__':
    main()
