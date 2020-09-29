﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.TestUtils;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Tests
{
    public class MultiThreadingTokenTests
    {
        [Theory, MemberData(nameof(MultiThreadingCreateAndVerifyTestCases))]
        public void MultiThreadingCreateAndVerify(MultiThreadingTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.MultiThreadingCreateAndVerify", theoryData);
            var numberOfErrors = 0;
            void action()
            {
                var jwt = theoryData.JwtSecurityTokenHandler.CreateEncodedJwt(theoryData.TokenDescriptor);
                var claimsPrincipal = theoryData.JwtSecurityTokenHandler.ValidateToken(theoryData.Jwt, theoryData.ValidationParameters, out SecurityToken _);
                var tokenValidationResult = theoryData.JsonWebTokenHandler.ValidateToken(theoryData.Jwt, theoryData.ValidationParameters);

                if (tokenValidationResult.Exception != null && tokenValidationResult.IsValid)
                        context.Diffs.Add("tokenValidationResult.IsValid, tokenValidationResult.Exception != null");

                if (!tokenValidationResult.IsValid)
                {
                    numberOfErrors++;
                    if (tokenValidationResult.Exception != null)
                        throw tokenValidationResult.Exception;
                    else
                        throw new SecurityTokenException("something failed");
                }
            }

            var actions = new Action[1000];
            for (int i = 0; i < actions.Length; i++)
                actions[i] = action;

            try
            {
                Parallel.Invoke(actions);
                theoryData.ExpectedException.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            if (numberOfErrors > 0)
                context.AddDiff($"Number of errors: '{numberOfErrors}'.");

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<MultiThreadingTheoryData> MultiThreadingCreateAndVerifyTestCases
        {
            get
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var jsonWebTokenHandler = new JsonWebTokenHandler();

                // ECD
                var tokenValidationParametersEcd = new TokenValidationParameters
                {
                    IssuerSigningKey = KeyingMaterial.Ecdsa256Key,
                    ValidAudience = Default.Audience,
                    ValidIssuer = Default.Issuer
                };

                var securityTokenDescriptorEcd = new SecurityTokenDescriptor
                {
                    Claims = Default.PayloadDictionary,
                    SigningCredentials = new SigningCredentials(KeyingMaterial.Ecdsa256Key, SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.Sha256),
                };

                var jwtEcd = jwtSecurityTokenHandler.CreateEncodedJwt(securityTokenDescriptorEcd);

                // RSA
                var securityTokenDescriptorRsa = new SecurityTokenDescriptor
                {
                    Claims = Default.PayloadDictionary,
                    SigningCredentials = new SigningCredentials(KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaSha256, SecurityAlgorithms.Sha256),
                };

                var tokenValidationParametersRsa = new TokenValidationParameters
                {
                    IssuerSigningKey = KeyingMaterial.RsaSecurityKey_2048,
                    ValidAudience = Default.Audience,
                    ValidIssuer = Default.Issuer
                };

                var jwtRsa = jwtSecurityTokenHandler.CreateEncodedJwt(securityTokenDescriptorRsa);

                // Symmetric
                var securityTokenDescriptorSymmetric = new SecurityTokenDescriptor
                {
                    Claims = Default.PayloadDictionary,
                    SigningCredentials = new SigningCredentials(KeyingMaterial.SymmetricSecurityKey2_256, SecurityAlgorithms.HmacSha256, SecurityAlgorithms.Sha256),
                };

                var tokenValidationParametersSymmetric = new TokenValidationParameters
                {
                    IssuerSigningKey = KeyingMaterial.SymmetricSecurityKey2_256,
                    ValidAudience = Default.Audience,
                    ValidIssuer = Default.Issuer
                };

                var jwtSymmetric = jwtSecurityTokenHandler.CreateEncodedJwt(securityTokenDescriptorSymmetric);

                // Encrypted "RSA keywrap"
                var securityTokenDescriptorEncryptedRsaKW = new SecurityTokenDescriptor
                {
                    Claims = Default.PayloadDictionary,
                    SigningCredentials = new SigningCredentials(KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaSha256, SecurityAlgorithms.Sha256),
                    EncryptingCredentials = new EncryptingCredentials(KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaOaepKeyWrap, SecurityAlgorithms.Aes128CbcHmacSha256)
                };

                var tokenValidationParametersEncryptedRsaKW = new TokenValidationParameters
                {
                    TokenDecryptionKey = KeyingMaterial.RsaSecurityKey_2048,
                    IssuerSigningKey = KeyingMaterial.RsaSecurityKey_2048,
                    ValidAudience = Default.Audience,
                    ValidIssuer = Default.Issuer
                };

                var jwtEncryptedRsaKW = jwtSecurityTokenHandler.CreateEncodedJwt(securityTokenDescriptorEncryptedRsaKW);

                // Encrypted "dir"
                var securityTokenDescriptorEncryptedDir = new SecurityTokenDescriptor
                {
                    Claims = Default.PayloadDictionary,
                    SigningCredentials = new SigningCredentials(KeyingMaterial.RsaSecurityKey_2048, SecurityAlgorithms.RsaSha256, SecurityAlgorithms.Sha256),
                    EncryptingCredentials = new EncryptingCredentials(KeyingMaterial.SymmetricSecurityKey2_256, "dir", SecurityAlgorithms.Aes128CbcHmacSha256)
                };

                var tokenValidationParametersEncryptedDir = new TokenValidationParameters
                {
                    TokenDecryptionKey = KeyingMaterial.SymmetricSecurityKey2_256,
                    IssuerSigningKey = KeyingMaterial.RsaSecurityKey_2048,
                    ValidAudience = Default.Audience,
                    ValidIssuer = Default.Issuer
                };

                var jwtEncryptedDir = jwtSecurityTokenHandler.CreateEncodedJwt(securityTokenDescriptorEncryptedDir);

                return new TheoryData<MultiThreadingTheoryData>()
                {
                    new MultiThreadingTheoryData
                    {
                        JwtSecurityTokenHandler = jwtSecurityTokenHandler,
                        JsonWebTokenHandler = jsonWebTokenHandler,
                        Jwt = jwtSymmetric,
                        TestId = "JwtSymmetric",
                        TokenDescriptor = securityTokenDescriptorSymmetric,
                        ValidationParameters = tokenValidationParametersSymmetric
                    },
                    new MultiThreadingTheoryData
                    {
                        JwtSecurityTokenHandler = jwtSecurityTokenHandler,
                        JsonWebTokenHandler = jsonWebTokenHandler,
                        Jwt = jwtRsa,
                        TestId = "JwtRsa",
                        TokenDescriptor = securityTokenDescriptorRsa,
                        ValidationParameters = tokenValidationParametersRsa
                    },
                    new MultiThreadingTheoryData
                    {
                        JwtSecurityTokenHandler = jwtSecurityTokenHandler,
                        JsonWebTokenHandler = jsonWebTokenHandler,
                        Jwt = jwtEcd,
                        TestId = "JwtEcd",
                        TokenDescriptor = securityTokenDescriptorEcd,
                        ValidationParameters = tokenValidationParametersEcd
                    },
                    new MultiThreadingTheoryData
                    {
                        JwtSecurityTokenHandler = jwtSecurityTokenHandler,
                        JsonWebTokenHandler = jsonWebTokenHandler,
                        Jwt = jwtEncryptedRsaKW,
                        TestId = "JwtRsaEncryptedRsaKW",
                        TokenDescriptor = securityTokenDescriptorEncryptedRsaKW,
                        ValidationParameters = tokenValidationParametersEncryptedRsaKW
                    },
                    new MultiThreadingTheoryData
                    {
                        JwtSecurityTokenHandler = jwtSecurityTokenHandler,
                        JsonWebTokenHandler = jsonWebTokenHandler,
                        Jwt = jwtEncryptedDir,
                        TestId = "JwtRsaEncryptedDir",
                        TokenDescriptor = securityTokenDescriptorEncryptedDir,
                        ValidationParameters = tokenValidationParametersEncryptedDir
                    },
                };
            }
        }
    }

    public class MultiThreadingTheoryData : TheoryDataBase
    {
        public string Jwt { get; set; }

        public SecurityTokenDescriptor TokenDescriptor { get; set; }

        public JsonWebTokenHandler JsonWebTokenHandler { get; set; }

        public JwtSecurityTokenHandler JwtSecurityTokenHandler { get; set; }

        public TokenValidationParameters ValidationParameters { get; set; }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
