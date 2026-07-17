# S3 Forensic Report

**Date:** 2026-07-17
**Scope:** Render backend, Amazon S3 integration, PostgreSQL upload metadata, and presigned object retrieval.

## Executive Result

**Root cause:** `S3_BUCKET_NAME` was configured as `rdcs-employee-storage`, while the bucket that exists in the AWS account is `rdcs-employee-storage-635379701165-ap-south-1-an`.

S3 returned HTTP `403` for the incorrect bucket name. This is expected S3 behavior: it does not disclose whether an inaccessible bucket exists. The IAM principal was valid, had `AmazonS3FullAccess`, and was not constrained by a permissions boundary.

**Fix applied:** Updated `S3_BUCKET_NAME` in Render to the exact bucket name from the bucket ARN. The local environment setting was updated to the same value.

**Final result:** S3 startup checks, PutObject, PostgreSQL metadata insertion, presigned URL generation, and S3 GetObject all passed.

---

## Runtime Configuration Evidence

| Field | Verified runtime value | Result |
|---|---|---|
| Bucket | `rdcs-employee-storage-635379701165-ap-south-1-an` | PASS |
| Region | `ap-south-1` | PASS |
| IAM access key | `AKIAZH*******` (masked) | PASS |
| IAM principal | `arn:aws:iam::635379701165:user/rdcs-storage-user` | PASS |
| IAM policy | `AmazonS3FullAccess` attached directly | PASS |
| Permissions boundary | Not set | PASS |

The startup diagnostic log confirmed the deployed backend loaded the corrected bucket and region.

## S3 Client Configuration

`AmazonS3Provider` creates the only source-level `S3Client` construction. It uses:

- `region: config.s3Region`
- `credentials.accessKeyId: config.s3AccessKeyId`
- `credentials.secretAccessKey: config.s3SecretAccessKey`
- `bucket: config.s3BucketName`

All bucket command inputs are sourced from `this.bucket`; no backend source occurrence hardcodes either S3 bucket name.

## Environment Variable Audit

The S3 configuration is loaded centrally from these environment variables:

| Variable | Configuration property |
|---|---|
| `S3_BUCKET_NAME` | `config.s3BucketName` |
| `S3_REGION` | `config.s3Region` |
| `S3_ACCESS_KEY_ID` | `config.s3AccessKeyId` |
| `S3_SECRET_ACCESS_KEY` | `config.s3SecretAccessKey` |

Source: `src/backend/src/config/index.ts`.

## AWS Startup Diagnostics

### Incorrect bucket configuration (before fix)

| Operation | Result |
|---|---|
| HeadBucket on `rdcs-employee-storage` | FAIL — HTTP 403 |
| ListObjectsV2 on `rdcs-employee-storage` | FAIL — HTTP 403 AccessDenied |

This occurred before application routes, upload handling, or PostgreSQL operations.

### Corrected bucket configuration (after fix)

| Operation | HTTP status | Result |
|---|---:|---|
| HeadBucket | 200 | PASS |
| ListObjectsV2 (`MaxKeys=5`) | 200 | PASS |
| ListObjectsV2 key count | 0 at startup | PASS |

The empty listing was correct at the moment of the startup probe; the final upload below created an object afterward.

## Object Key Format

The backend constructs keys as:

```text
{companyId}/{employeeId}/{UTC yyyy}/{UTC MM}/{UTC dd}/{UTC HH}/{UTC mm}/{fileName}
```

The implementation is in `AmazonS3Provider.buildObjectKey`.

The same `s3ObjectKey` returned by PutObject is inserted into `UploadedFile.s3ObjectKey`, then used by `StorageController.viewScreenshot` to create the GetObject presigned URL.

## Final Live End-to-End Test

A valid minimal JPEG payload was uploaded through the production API.

### PutObject / upload API

| Field | Value |
|---|---|
| HTTP result | `201 Created` |
| API result | `Screenshot uploaded successfully` |
| Upload ID | `de1937d4-2294-4239-abe4-a105a18089c6` |
| Bucket | `rdcs-employee-storage-635379701165-ap-south-1-an` |
| Object key | `company-rdcs-001/b8f90fd4-90ac-4c69-b577-59fea38e9419/2026/07/17/16/23/s3-forensic-final.jpg` |
| File size | `14` bytes |
| SHA-256 verified | `true` |
| ETag | `6a6f31eb4bda76f4e83cd5a553d315e3` |

### PostgreSQL `UploadedFile` evidence

| Database field | Verified value | Result |
|---|---|---|
| `s3Bucket` | Corrected bucket name | PASS |
| `s3ObjectKey` | Final test object key | PASS |
| `checksum` | SHA-256 persisted | PASS |
| `fileSize` | `14` | PASS |
| `uploadedAt` | `2026-07-17T16:23:33.453Z` | PASS |
| `storageProvider` | Not a column on `UploadedFile` | N/A |

`storageProvider` belongs to the separate screenshot metadata model, not the backend `UploadedFile` table.

### Requested-versus-stored key comparison

| Source | Key |
|---|---|
| Upload API response | `company-rdcs-001/b8f90fd4-90ac-4c69-b577-59fea38e9419/2026/07/17/16/23/s3-forensic-final.jpg` |
| PostgreSQL `s3ObjectKey` | `company-rdcs-001/b8f90fd4-90ac-4c69-b577-59fea38e9419/2026/07/17/16/23/s3-forensic-final.jpg` |

**Comparison result: exact match.**

### Presigned URL / GetObject

| Operation | Result |
|---|---|
| Presigned URL generated for stored `s3ObjectKey` | PASS |
| Credential embedded in presign | Active configured key, masked in report |
| Direct GET on generated URL | HTTP 200 |
| Bytes returned | 14 |

## Catch Block Audit

- `StorageService`: no catch blocks; errors propagate.
- `AmazonS3Provider`: diagnostic and presign errors log AWS name, code, HTTP status, request IDs, extended request IDs, and stack trace.
- `StorageController`: all catch blocks log the full error object and stack before returning the existing API response.
- `UploadQueueService`: no catch blocks.
- Desktop `UploadWorker`: C# exception handling logs `Exception` through the configured Serilog logger.

## Final Status

| Stage | Result |
|---|---|
| Environment variables loaded | PASS |
| Bucket / region configuration | PASS |
| IAM credential in use | PASS |
| HeadBucket | PASS |
| ListObjectsV2 | PASS |
| PutObject | PASS |
| Database metadata insertion | PASS |
| Stored/requested key equality | PASS |
| Presigned URL generation | PASS |
| GetObject via presigned URL | PASS |

## Follow-up

The forensic diagnostic logs currently emit presigned URLs. These are temporary credentials and should be removed after this investigation. Rotate the AWS secret access key exposed during earlier local diagnostic output.
